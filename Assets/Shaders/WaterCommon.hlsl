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
    float   _SkyBoxReflectSmooth;
    float   _RefractionPower;
    float   _FoamPower;
CBUFFER_END

TEXTURE2D(_BaseMap);                SAMPLER(sampler_BaseMap);
TEXTURE2D(_WaveNormalMap1);         SAMPLER(sampler_WaveNormalMap1);
TEXTURE2D(_WaveNormalMap2);         SAMPLER(sampler_WaveNormalMap2);
TEXTURE2D(_FoamTex);                SAMPLER(sampler_FoamTex);
TEXTURECUBE(_SkyBox);               SAMPLER(sampler_SkyBox);
TEXTURE2D(_CameraOpaqueTexture);    SAMPLER(sampler_CameraOpaqueTexture);

float GetReflectionCoefficient(float3 viewDir, float3 normal){
    float a = 1 - dot(viewDir, normal);
    return pow(a, _FresnelPower);
}

half3 WaterSpecular(float3 viewDir, float3 lightDir, half3 lightColor, float3 normal){
    float3 halfDir = normalize(lightDir + viewDir);
    float nl = max(0,dot(halfDir,normal));
    return _Gloss * pow(nl, _Shininess) * lightColor;
}

half3 WaterDiffuse(float shadowAttenuation, float3 lightDir, half3 lightColor, float3 normal){
    return saturate(dot(normal, lightDir) * lightColor) * shadowAttenuation;
}

half3 GetSkyBoxColor(float3 viewDir, float3 normal){
    float3 adjustNormal = normal;
    adjustNormal.xz /= _SkyBoxReflectSmooth;
    float3 reflectionDir = reflect(viewDir, normal);
    return SAMPLE_TEXTURECUBE(_SkyBox, sampler_SkyBox, reflectionDir).rgb;
}

half3 GetRefractionColor(float2 screenUV, float3 normal){
    float2 refractionUV = normal.xz * _RefractionPower + screenUV;
    half4 refractionColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, refractionUV);
    return refractionColor;
}

float GetFoamStrength(float4 positionCS, float2 screenUV){
    float depth = SampleSceneDepth(screenUV);
    float zDepth = positionCS.z / positionCS.w;
    float dis = abs(zDepth - depth);
    return pow(max(0,dis / lerp(0,1,_FoamPower)),4);
}

#endif