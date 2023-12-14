#ifndef WATER_COMMON_INCLUDED
#define WATER_COMMON_INCLUDED

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv           : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
    float3 normalWS     : TEXCOORD1;
    float3 positionWS   : TEXCOORD2;
};

CBUFFER_START(UnityPerMaterial)
    float4  _BaseMap_ST;
    float4  _ShallowWaterColor;
    float4  _DeepWaterColor;
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
    float   _CausticsPower;
    float   _DepthDistancePower;
CBUFFER_END

float4x4 _MatrixVP;
float4x4 _MatrixInvVP;

TEXTURE2D(_BaseMap);                SAMPLER(sampler_BaseMap);
TEXTURE2D(_WaveNormalMap1);         SAMPLER(sampler_WaveNormalMap1);
TEXTURE2D(_WaveNormalMap2);         SAMPLER(sampler_WaveNormalMap2);
TEXTURE2D(_FoamTex);                SAMPLER(sampler_FoamTex);
TEXTURE2D(_CausticsTex);            SAMPLER(sampler_CausticsTex);
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

float3 TransformCSToWS(float2 screenUV, float depth){
    float3 positionCS = float3(screenUV * 2 - 1, depth);
    float4 positionWS = mul(_MatrixInvVP, float4(positionCS, 1));
    positionWS /= positionWS.w;
    return positionWS.xyz;
}

half3 GetCausticsColor(float2 screenUV, float depth, float3 normal){
    float3 positionWS = TransformCSToWS(screenUV, depth);
    float2 causticsUV = normal.xz * 0.25 + positionWS.xz;
    half4 causticsColor = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, causticsUV * 0.4);
    return causticsColor.rgb;
}

float GetPosDistanceWS(float3 positionWS, float2 screenUV, float depth){
    float3 behindPositionWS = TransformCSToWS(screenUV, depth);
    float dis = distance(positionWS, behindPositionWS);
    return dis;
}

float GetFoamStrength(float distance){
    return pow(max(0, 1 - distance / lerp(0.1, 1, _FoamPower)), 4);
}

#endif