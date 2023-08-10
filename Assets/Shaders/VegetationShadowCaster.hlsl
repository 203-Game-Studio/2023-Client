#ifndef VEGETATION_SHADOWCASTER_INCLUDED
#define VEGETATION_SHADOWCASTER_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

////////////////////////////////////////////////////////////////////////////
//ShadowCaster Pass
////////////////////////////////////////////////////////////////////////////
struct ShadowCasterAttributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 color        : COLOR;
    float2 texcoord     : TEXCOORD0;
};

struct ShadowCasterVaryings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
    float4 screenPos    : TEXCOORD1;
#if defined(VEGETATION_LEAVES)
    float3 positionWS   : TEXCOORD2;
#endif
};

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseColor;

    float  _WindTrunkContrast;
	float  _WindTrunkPosition;
	float  _WindMultiplier;
	float  _MicroWindMultiplier;
    
	float  _HidePower;
CBUFFER_END

//风相关
float _WindSpeed;
float _WindPower;
float _WindBurstsSpeed;
float _WindBurstsScale;
float _WindBurstsPower;
#if defined(VEGETATION_LEAVES)
float _MicroFrequency;
float _MicroSpeed;
float _MicroPower;
#endif

float3 _LightDirection;

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

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

float4 GetWindOffset(float4 positionOS, float4 color, float3 normalOS)
{
    //大风
    float windSpeed = _TimeParameters.x * _WindSpeed;
    float3 worldPos = mul(GetObjectToWorldMatrix(), positionOS).xyz;
    float2 newWorldPos = _Time.y * float2(_WindBurstsSpeed, _WindBurstsSpeed) + worldPos.xz;
    float prelinNoiseVal = snoise(newWorldPos * (_WindBurstsScale / 10.0));
    prelinNoiseVal = prelinNoiseVal * 0.5 + 0.5;
    prelinNoiseVal = _WindPower * prelinNoiseVal * _WindBurstsPower;
    float4 posTarget = pow(max(1.0 - color.b, 0.0001), _WindTrunkPosition).xxxx;
    posTarget = saturate(CalculateContrast(_WindTrunkContrast, posTarget));
    float3 baseWindWorldPos = float3(((sin(windSpeed) * prelinNoiseVal) * posTarget).r, 0.0, ((cos(windSpeed) * (prelinNoiseVal * 0.5)) * posTarget).r);
    float4 baseWind = mul(GetWorldToObjectMatrix(),float4(baseWindWorldPos, 0.0)) * _WindMultiplier;

#if defined(VEGETATION_LEAVES)
    //微风
    prelinNoiseVal = snoise(_Time.y * _MicroSpeed.xx + worldPos.xz);
    prelinNoiseVal = prelinNoiseVal * 0.5 + 0.5;
    float3 microWindFactor = clamp(sin((_MicroFrequency * (worldPos + prelinNoiseVal))), float3(-1,-1,-1), float3(1,1,1));
    float3 microWind = microWindFactor * normalOS * _MicroPower * color.r * _MicroWindMultiplier;
#endif

#if defined(VEGETATION_LEAVES)
    return baseWind + float4(microWind, 0.0);
#else
    return baseWind;
#endif
}

ShadowCasterVaryings ShadowPassVertex(ShadowCasterAttributes input)
{
    ShadowCasterVaryings output = (ShadowCasterVaryings)0;

    float4 offset = GetWindOffset(input.positionOS, input.color, input.normalOS);

    //将偏移加到局部空间坐标上
    input.positionOS = input.positionOS + offset;

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
	float3 positionWS = TransformObjectToWorld(input.positionOS);
#if defined(VEGETATION_LEAVES)
	output.positionWS = positionWS;
#endif

	float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
    #if UNITY_REVERSED_Z
		positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
	#else
		positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
	#endif

    output.screenPos = ComputeScreenPos(positionCS);
	output.positionCS = positionCS;

    return output;
}

ShadowCasterVaryings DepthOnlyVertex(ShadowCasterAttributes input)
{
    ShadowCasterVaryings output = (ShadowCasterVaryings)0;

    float4 offset = GetWindOffset(input.positionOS, input.color, input.normalOS);

    //将偏移加到局部空间坐标上
    input.positionOS = input.positionOS + offset;

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
	float3 positionWS = TransformObjectToWorld(input.positionOS);
#if defined(VEGETATION_LEAVES)
	output.positionWS = positionWS;
#endif

    output.positionCS = TransformWorldToHClip(positionWS);
    output.screenPos = ComputeScreenPos(output.positionCS);

    return output;
}

float GetDitherVal(float2 uv, float4 screenPos
#if defined(VEGETATION_LEAVES)
, float3 positionWS
#endif
){
    float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);

#if defined(VEGETATION_LEAVES)
	float3 viewDirectionWS = normalize(_WorldSpaceCameraPos.xyz  - positionWS);
#endif

    //渐隐
    float4 screenPosNorm = screenPos / screenPos.w;
	screenPosNorm.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? screenPosNorm.z : screenPosNorm.z * 0.5 + 0.5;
	float2 clipScreen = screenPosNorm.xy * _ScreenParams.xy;
	float ditherVal = Dither8x8Bayer(fmod(clipScreen.x, 8), fmod(clipScreen.y, 8));
#if defined(VEGETATION_LEAVES)
	float3 ditherNormal = normalize(cross(ddy(positionWS), ddx(positionWS)));
	float vDotD = abs(dot(viewDirectionWS, ditherNormal));
	float ditherResult = saturate((albedo.a * (vDotD * 2.0 - 1.0) * _HidePower));
#else
	float ditherResult = saturate((albedo.a * _HidePower));
#endif
	ditherVal = step(ditherVal, ditherResult);
    return ditherVal;
}

half4 ShadowPassFragment(ShadowCasterVaryings input) : SV_Target
{
    float ditherVal = GetDitherVal(input.uv, input.screenPos
    #if defined(VEGETATION_LEAVES)
    , input.positionWS
    #endif
    );
    //clip
    clip(ditherVal - 0.25);

    return 0;
}

half DepthOnlyFragment(ShadowCasterVaryings input) : SV_TARGET
{
    float ditherVal = GetDitherVal(input.uv, input.screenPos
    #if defined(VEGETATION_LEAVES)
    , input.positionWS
    #endif
    );
    //clip
    clip(ditherVal - 0.25);
    return input.positionCS.z;
}

#endif