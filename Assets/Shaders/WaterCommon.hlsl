#ifndef WATER_COMMON_INCLUDED
#define WATER_COMMON_INCLUDED

struct Attributes
{
    float4 positionOS   : POSITION;
    float2 uv           : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
    float3 positionWS   : TEXCOORD1;
};

CBUFFER_START(UnityPerMaterial)
    float4  _BaseMap_ST;
    float4  _BaseColor;
    //float4  _WaveNormalMap_ST;
    float   _WaveNormalScale1;
    float   _WaveNormalScale2;
    float   _WaveXSpeed;
    float   _WaveYSpeed;
    float   _Gloss;
    float   _Shininess;
    float   _FresnelPower;
CBUFFER_END

TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_WaveNormalMap1);  SAMPLER(sampler_WaveNormalMap1);
TEXTURE2D(_WaveNormalMap2);  SAMPLER(sampler_WaveNormalMap2);

float GetReflectionCoefficient(float3 viewDir, float3 normal){
    float a = 1 - dot(viewDir, normal);
    return pow(a, _FresnelPower);
}

half3 WaterSpecular(float3 viewDir, float3 lightDir, half3 lightColor, float3 normal){
    float3 halfDir = normalize(lightDir + viewDir);
    float nl = max(0,dot(halfDir,normal));
    return _Gloss * pow(nl, _Shininess) * lightColor;
}

#endif