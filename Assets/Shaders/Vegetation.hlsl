#ifndef VEGETATION_INCLUDED
#define VEGETATION_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

////////////////////////////////////////////////////////////////////////////
//Forword Lit Pass
////////////////////////////////////////////////////////////////////////////
struct LitAttributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float4 color        : COLOR;
};

struct LitVaryings
{
    float4 positionCS   : SV_POSITION;
	float2 uv           : TEXCOORD0;
    float3 positionWS   : TEXCOORD1;
	float3 normalWS     : TEXCOORD2;
	float4 color        : TEXCOORD3;
    float4 screenPos    : TEXCOORD4;
};

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseColor;
	float _HidePower;

	float4 _GradientColor;
	float  _GradientFalloff;
	float  _GradientPosition;

	float4 _ColorVariation;
	float _ColorVariationPower;
	float _NoiseScale;

	float  _Scattering;
	float  _Cutoff;
    float  _TransStrength;
	float  _TransNormal;
	float  _TransScattering;
	float  _TransDirect;
	float  _TransAmbient;
	float  _TransShadow;
CBUFFER_END

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_ColorVariationNoise);
SAMPLER(sampler_ColorVariationNoise);

inline float Dither8x8Bayer( int x, int y )
{
	const float dither[ 64 ] = {
        1, 49, 13, 61,  4, 52, 16, 64,
        33, 17, 45, 29, 36, 20, 48, 32,
        9, 57,  5, 53, 12, 60,  8, 56,
        41, 25, 37, 21, 44, 28, 40, 24,
        3, 51, 15, 63,  2, 50, 14, 62,
        35, 19, 47, 31, 34, 18, 46, 30,
        11, 59,  7, 55, 10, 58,  6, 54,
        43, 27, 39, 23, 42, 26, 38, 22};
	int r = y * 8 + x;
	return dither[r] / 64;
}

LitVaryings LitPassVertex(LitAttributes input)
{
    LitVaryings output = (LitVaryings)0;

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

	output.positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(output.positionWS);
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
	output.color = input.color;
    output.screenPos = ComputeScreenPos(output.positionCS);

    return output;
}

half4 LitPassFragment(LitVaryings input) : SV_Target
{
    float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

	float3 viewDirectionWS = normalize(_WorldSpaceCameraPos.xyz  - input.positionWS);

    //渐隐和采样去除生硬的边
    float4 screenPosNorm = input.screenPos / input.screenPos.w;
	screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? screenPosNorm.z : screenPosNorm.z * 0.5 + 0.5;
	float2 clipScreen = screenPosNorm.xy * _ScreenParams.xy;
	float ditherVal = Dither8x8Bayer( fmod(clipScreen.x, 8), fmod(clipScreen.y, 8) );
	float3 ditherNormal = normalize(cross(ddy(input.positionWS), ddx(input.positionWS)));
	float vDotD = abs(dot(viewDirectionWS, ditherNormal));
	float ditherResult = saturate((albedo.a * (vDotD * 2.0 - 1.0) * _HidePower));
	ditherVal = step(ditherVal, ditherResult);

    //clip
    clip(ditherVal - 0.25);

    float3 normalWS = normalize(input.normalWS);

    //梯度颜色插值
    float gradientLerpVal = saturate( ( ( -1.0 - normalWS.y + _GradientPosition * 3.0 ) / _GradientFalloff ));
    float4 gradientLerpColor = lerp( _BaseColor , _GradientColor , gradientLerpVal);

    //世界uv 模拟云
    float4 cloudLerpVal = saturate(lerp(_ColorVariation, _ColorVariation / max(1.0 - gradientLerpColor, 0.00001), _ColorVariationPower));
    float4 noiseVal = max(SAMPLE_TEXTURE2D(_ColorVariationNoise, sampler_ColorVariationNoise, float2(input.positionWS.x, input.positionWS.z) * _NoiseScale), 0.0001);
    float4 cloudVal = lerp(gradientLerpColor, cloudLerpVal,  _ColorVariationPower * pow(noiseVal, 3));

    albedo *= cloudVal;

    //GI
    float3 SH = SampleSH(normalWS);

    InputData inputData;
    inputData.positionWS = input.positionWS;
	inputData.viewDirectionWS = viewDirectionWS;
    inputData.normalWS = normalWS;
    inputData.bakedGI = SH;
	inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);

    SurfaceData surfaceData;
    surfaceData.albedo = albedo;
    surfaceData.specular = 0.5;
    surfaceData.metallic = 0;
    surfaceData.smoothness = 0;
    surfaceData.normalTS = half3(0, 0, 1);
    surfaceData.emission = 0;
    surfaceData.occlusion = 1;
    surfaceData.alpha = albedo.a;
    surfaceData.clearCoatMask = 0;
    surfaceData.clearCoatSmoothness = 1;

    //先用URP PBR计算 后面可以改成Lambert +（菲涅尔+GGX）/（或者NdotH替代）
    half4 color = UniversalFragmentPBR(inputData, surfaceData);

    //次表面散射
    Light mainLight = GetMainLight(inputData.shadowCoord);
	float3 mainAtten = mainLight.color * mainLight.distanceAttenuation;
    mainAtten = lerp( mainAtten, mainAtten * mainLight.shadowAttenuation, _TransShadow);
    half3 mainLightDir = mainLight.direction + normalWS * _TransNormal;
    half mainLightVdotL = pow(saturate(dot(viewDirectionWS, -mainLightDir)), _TransScattering);
    half3 mainLightTranslucency = mainAtten * (mainLightVdotL * _TransDirect + inputData.bakedGI * _TransAmbient) * _Scattering.xxx;
    color.rgb += albedo * mainLightTranslucency * _TransStrength;

    return color;
}

#endif