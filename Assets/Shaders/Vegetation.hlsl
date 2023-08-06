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
};

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseColor;
	float4 _GradientColor;
	float  _GradientFalloff;
	float  _GradientPosition;

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

LitVaryings LitPassVertex(LitAttributes input)
{
    LitVaryings output = (LitVaryings)0;

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

	output.positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(output.positionWS);
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
	output.color = input.color;

    return output;
}

half4 LitPassFragment(LitVaryings input) : SV_Target
{
    float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

    clip(albedo.a - 0.25);

    float3 normalWS = normalize(input.normalWS);
	float3 vieDirectionWS = normalize(_WorldSpaceCameraPos.xyz  - input.positionWS);

    //梯度颜色插值
    float gradientLerpVal = saturate(((1.0 - normalWS.y) + _GradientPosition - 2.0) / _GradientFalloff);
    float4 gradientLerpColor = lerp( _BaseColor , _GradientColor , gradientLerpVal);
    albedo *= gradientLerpColor;

    float3 SH = SampleSH(normalWS);
    float4 shadowCoords = TransformWorldToShadowCoord(input.positionWS);

    InputData inputData;
    inputData.positionWS = input.positionWS;
	inputData.viewDirectionWS = vieDirectionWS;
    inputData.normalWS = normalWS;
    inputData.bakedGI = SH;
	inputData.shadowCoord = shadowCoords;

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

    Light mainLight = GetMainLight(inputData.shadowCoord);
	float3 mainAtten = mainLight.color * mainLight.distanceAttenuation;
    mainAtten = lerp( mainAtten, mainAtten * mainLight.shadowAttenuation, _TransShadow);

    //transmission
    half3 mainTransmission = max(0 , -dot(normalWS, mainLight.direction)) * mainAtten;
    color.rgb += albedo * mainTransmission;
    
    //scattering
    half3 mainLightDir = mainLight.direction + normalWS * _TransNormal;
    half mainLightVdotL = pow(saturate(dot(vieDirectionWS, -mainLightDir)), _TransScattering);
    half3 mainLightTranslucency = mainAtten * (mainLightVdotL * _TransDirect/* + inputData.bakedGI * ambient */) * _Scattering.xxx;
    color.rgb += albedo * mainLightTranslucency * _TransStrength;

    return color;
}

#endif