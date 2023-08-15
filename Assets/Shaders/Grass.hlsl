#ifndef GRASS_INCLUDED
#define GRASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

////////////////////////////////////////////////////////////////////////////
//Forword Lit Pass
////////////////////////////////////////////////////////////////////////////
struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    uint   instanceID   : SV_InstanceID;
    float2 uv           : TEXCOORD0;
};
struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
    float3 normalWS     : TEXCOORD1;
    float4 positionWS   : TEXCOORD2;
};

CBUFFER_START(UnityPerMaterial)
    float4  _BaseMap_ST;
    float4  _DiffuseColorLow;
    float4  _DiffuseColorMid;
    float4  _DiffuseColorHigh;
    float4  _SpecularColor;
    float   _Cutoff;
    float   _Roughness;

    float2  _GrassQuadSize;//这个修改进compute buffer里
    float4x4 _TerrianLocalToWorld;//这个后面得干掉，改成传世界空间位置

    float4  _ScatteringColor;
    float   _TransStrength;
    float   _TransNormal;
    float   _TransScattering;
    float   _TransDirect;
    float   _TransShadow;
CBUFFER_END
float4  _Wind;
float   _MicroFrequency;
float   _MicroSpeed;
float   _MicroPower;

float _GrassBendingPositionsNum;
float4 _GrassBendingPositions[8];

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    struct GrassInfo{
        float4x4 localToTerrian;
        float4 texParams;
    };
    StructuredBuffer<GrassInfo> _GrassInfos;
#endif

void setup(){}

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

float3 CalculateBendingFloat(float3 WorldPosition, float MaxDistance, float Falloff, float PushAwayStrength, float PushDownStrength/*,
    out float3 Offset, out float WindMultiplier*/) {
    float3 offset = 0;
    //WindMultiplier = 1;
    for (int i = 0; i < _GrassBendingPositionsNum; i++) {
        float3 objectPositionWS = _GrassBendingPositions[i].xyz;
        float3 distanceVector = WorldPosition - objectPositionWS;
        float distance = length(distanceVector);
        float strength = 1 - pow(saturate(distance / MaxDistance), Falloff);
        float3 xzDistance = distanceVector;
        xzDistance.y = 0;
        float3 pushAwayOffset = normalize(xzDistance) * PushAwayStrength * strength;
        float3 squishOffset = float3(0, -1, 0) * PushDownStrength * strength;
        offset += pushAwayOffset + squishOffset;
        //WindMultiplier = min(WindMultiplier, 1 - strength);
    }
    return offset;
}

float3 ApplyForce(float3 positionWS, float3 grassUpWS, float3 forceDir, float strength, float vertexLocalHeight)
{
    //计算草弯曲角度,0-90
    float rad = strength * PI * 0.9 / 2;
    //得到force与grassUpWS的正交向量
    forceDir = normalize(forceDir - dot(forceDir,grassUpWS) * grassUpWS);
    float x, y;  
    //x为单位球在wind方向，y为grassUp方向
    sincos(rad, x, y);
    float3 windedPos = x * forceDir + y * grassUpWS;

    return positionWS + (windedPos - grassUpWS) * vertexLocalHeight;
}

Varyings LitPassVertex(Attributes input)
{
    Varyings output;
    input.positionOS.xy = input.positionOS.xy * _GrassQuadSize;

    float3 grassUpDir = float3(0,1,0);

    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        //将顶点和法线从草quad本地空间转换到Terrian本地空间
        GrassInfo grassInfo = _GrassInfos[input.instanceID];
        input.positionOS = mul(grassInfo.localToTerrian, input.positionOS);
        input.normalOS = mul(grassInfo.localToTerrian, float4(input.normalOS, 0)).xyz;
        grassUpDir = mul(grassInfo.localToTerrian, float4(grassUpDir,0)).xyz;
        input.uv = input.uv * grassInfo.texParams.xy + grassInfo.texParams.zw;

    #endif
    float4 positionWS = mul(_TerrianLocalToWorld, input.positionOS);
    positionWS /= positionWS.w;
    grassUpDir = normalize(mul(_TerrianLocalToWorld,float4(grassUpDir,0)));

    //微风
    float prelinNoiseVal = snoise(_Time.y * _MicroSpeed.xx + positionWS.xz) * 0.8;
    prelinNoiseVal = prelinNoiseVal * 0.5 + 0.5;
    //计算草风吹后的新位置 噪声乘个0.7减弱下
    positionWS.xyz = ApplyForce(positionWS.xyz, grassUpDir, normalize(_Wind.xyz), prelinNoiseVal*0.7, input.positionOS.y);

    float3 offset = 0;
    float strength = 0;
    for (int i = 0; i < _GrassBendingPositionsNum; i++) {
        float3 objectPositionWS = _GrassBendingPositions[i].xyz;
        float3 distanceVector = positionWS - objectPositionWS;
        float distance = length(distanceVector);
        strength += 1 - pow(saturate(distance / 1), 2);
        distanceVector.y = 0;
        offset += distanceVector;
    }
    //计算踩踏草的力
    positionWS.xyz = ApplyForce(positionWS.xyz, grassUpDir, normalize(offset), strength, input.positionOS.y);
    
    output.positionWS = positionWS;
    output.positionCS = TransformWorldToHClip(positionWS);
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.uv = input.uv;
    return output;
}

float F_FrenelSchlick(float cosTheta, float3 F0)
{
	return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
}

float D_DistributionGGX(float3 N, float3 H, float roughness)
{
    float a      = roughness * roughness;
    float a2     = a * a;
    float NdotH  = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;
    float nom   = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = 3.14159265359 * denom * denom;
    return nom / denom;
}

float GGX_DistanceFade(float3 N,float3 V,float3 L,float Roughness,float DistanceFade)
{
    float3 H = normalize(L+V);
    float D = D_DistributionGGX(N, H, Roughness);
    float F = F_FrenelSchlick(saturate(dot(N, V)), 0.04);

    return D * F * DistanceFade;
}

half4 LitPassFragment(Varyings input) : SV_Target
{
    half4 albedo = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv);
    clip(albedo.a - _Cutoff);

    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
	float3 viewDirectionWS = normalize(_WorldSpaceCameraPos.xyz  - input.positionWS);
    Light mainLight = GetMainLight(shadowCoord);
	float3 mainAtten = mainLight.color * mainLight.distanceAttenuation;
    mainAtten = lerp(mainAtten, mainAtten * mainLight.shadowAttenuation, _TransShadow);
    
    //specular
    float ggx = GGX_DistanceFade(float3(0, 1, 0), viewDirectionWS, mainLight.direction, _Roughness, _WorldSpaceCameraPos.xyz  - input.positionWS);
    float3 specularColor = saturate(ggx) * _SpecularColor * mainAtten;

    //diffuse
    float grassHeight = input.uv.y * 0.5;
    float lerpVal1 = grassHeight;
    float lerpVal2 = grassHeight + 0.5;
    float3 color1 = lerp(_DiffuseColorLow, _DiffuseColorMid, lerpVal1);
    float3 color2 = lerp(_DiffuseColorMid, _DiffuseColorHigh, lerpVal2);
    float3 diffuseColor = lerp(color1, color2, input.uv.y) * albedo.rgb;
    
    //sss
    float3 mainLightDir = mainLight.direction + float3(0, 1, 0) * _TransNormal;
    float mainLightVdotL = pow(saturate(dot(viewDirectionWS, -mainLightDir)), _TransScattering);
    float3 mainLightTranslucency = mainAtten * mainLightVdotL * _TransDirect;
    float3 scatteringColor = _ScatteringColor.rgb * albedo.rgb * mainLightTranslucency * _TransStrength;
    scatteringColor = scatteringColor * lerp(0, 1, input.uv.y);

    float3 finalColor = lerp(diffuseColor, specularColor + scatteringColor, _Roughness);

    return float4(finalColor, 1);
}

#endif