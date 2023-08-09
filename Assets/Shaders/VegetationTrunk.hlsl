#ifndef VEGETATION_TRUNK_INCLUDED
#define VEGETATION_TRUNK_INCLUDED

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
	float4 tangentWS     : TEXCOORD3;
    float4 screenPos    : TEXCOORD4;
};

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseColor;
	float  _NormalStrength;
	float  _HidePower;

    float  _WindTrunkContrast;
	float  _WindTrunkPosition;
	float  _WindMultiplier;
	float  _MicroWindMultiplier;
CBUFFER_END

//风相关
float _WindSpeed;
float _WindPower;
float _WindBurstsSpeed;
float _WindBurstsScale;
float _WindBurstsPower;
float _MicroFrequency;
float _MicroSpeed;
float _MicroPower;

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);

float3 mod2D289(float3 x) 
{ 
    return x - floor(x * (1.0 / 289.0)) * 289.0; 
}

float2 mod2D289(float2 x) 
{ 
    return x - floor(x * (1.0 / 289.0)) * 289.0; 
}

float3 permute(float3 x) 
{
    return mod2D289(((x * 34.0) + 1.0) * x); 
}

inline float Dither8x8Bayer(int x, int y)
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

float snoise(float2 v)
{
	const float4 C = float4(0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439);
	float2 i = floor(v + dot(v, C.yy));
	float2 x0 = v - i + dot(i, C.xx);
	float2 i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
	float4 x12 = x0.xyxy + C.xxzz;
	x12.xy -= i1;
	i = mod2D289(i);
	float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0)) + i.x + float3(0.0, i1.x, 1.0));
	float3 m = max(0.5 - float3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);
	m = m * m;
	m = m * m;
	float3 x = 2.0 * frac(p * C.www) - 1.0;
	float3 h = abs(x) - 0.5;
	float3 ox = floor(x + 0.5);
	float3 a0 = x - ox;
	m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);
	float3 g;
	g.x = a0.x * x0.x + h.x * x0.y;
	g.yz = a0.yz * x12.xz + h.yz * x12.yw;
	return 130.0 * dot(m, g);
}

float4 CalculateContrast(float contrastValue, float4 colorTarget)
{
	float t = 0.5 * (1.0 - contrastValue);
	return mul(float4x4(contrastValue,0,0,t, 0,contrastValue,0,t, 0,0,contrastValue,t, 0,0,0,1), colorTarget);
}

LitVaryings LitPassVertex(LitAttributes input)
{
    LitVaryings output = (LitVaryings)0;

    //大风
    float windSpeed = _TimeParameters.x * _WindSpeed;
    float3 worldPos = mul(GetObjectToWorldMatrix(), input.positionOS).xyz;
    float2 newWorldPos = _Time.y * float2(_WindBurstsSpeed, _WindBurstsSpeed) + worldPos.xz;
    float prelinNoiseVal = snoise(newWorldPos * (_WindBurstsScale / 10.0));
    prelinNoiseVal = prelinNoiseVal * 0.5 + 0.5;
    prelinNoiseVal = _WindPower * prelinNoiseVal * _WindBurstsPower;
    float4 posTarget = pow(max(1.0 - input.color.b, 0.0001), _WindTrunkPosition).xxxx;
    posTarget = saturate(CalculateContrast(_WindTrunkContrast, posTarget));
    float3 baseWindWorldPos = float3(((sin(windSpeed) * prelinNoiseVal) * posTarget).r, 0.0, ((cos(windSpeed) * (prelinNoiseVal * 0.5)) * posTarget).r);
    float4 baseWind = mul(GetWorldToObjectMatrix(),float4(baseWindWorldPos, 0.0)) * _WindMultiplier;

    //将偏移加到局部空间坐标上
    input.positionOS = input.positionOS + baseWind;

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
	output.positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(output.positionWS);
    output.screenPos = ComputeScreenPos(output.positionCS);

	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
	output.tangentWS.xyz = TransformObjectToWorldDir(input.tangentOS);
    output.tangentWS.w = input.tangentOS.w;

    return output;
}

half4 LitPassFragment(LitVaryings input) : SV_Target
{
    float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
	float3 viewDirectionWS = normalize(_WorldSpaceCameraPos.xyz  - input.positionWS);

    half sign = input.tangentWS.w * unity_WorldTransformParams.w;
    half3 bitangentWS = cross(input.normalWS, input.tangentWS) * sign;
    half3x3 TBN = half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS);
    float3 normalVal = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _NormalStrength);
    half3 normalWS = TransformTangentToWorld(normalVal, TBN);

    //渐隐和采样去除生硬的边
    float4 screenPosNorm = input.screenPos / input.screenPos.w;
	screenPosNorm.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? screenPosNorm.z : screenPosNorm.z * 0.5 + 0.5;
	float2 clipScreen = screenPosNorm.xy * _ScreenParams.xy;
	float ditherVal = Dither8x8Bayer(fmod(clipScreen.x, 8), fmod(clipScreen.y, 8));
	float ditherResult = saturate((albedo.a * _HidePower));
	ditherVal = step(ditherVal, ditherResult);

    //clip
    clip(ditherVal - 0.25);

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
    surfaceData.alpha = 1;
    surfaceData.clearCoatMask = 0;
    surfaceData.clearCoatSmoothness = 1;

    half4 color = UniversalFragmentPBR(inputData, surfaceData);

    return color;
}

#endif