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
    float4  _BaseColor;
    float4  _ColorTop;
    float   _Cutoff;

    float2  _GrassQuadSize;//这个修改进compute buffer里
    float4  _Wind;
    float   _WindNoiseStrength;
    float4x4 _TerrianLocalToWorld;//这个后面得干掉，改成传世界空间位置

    float4  _ScatteringColor;
    float   _TransStrength;
    float   _TransNormal;
    float   _TransScattering;
    float   _TransDirect;
    float   _TransShadow;
CBUFFER_END

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

sampler2D _NoiseMap;

#define StormFront _StormParams.x
#define StormMiddle _StormParams.y
#define StormEnd _StormParams.z
#define StormSlient _StormParams.w

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    struct GrassInfo{
        float4x4 localToTerrian;
        float4 texParams;
    };
    StructuredBuffer<GrassInfo> _GrassInfos;
#endif

void setup(){}

float3 ApplyWind(float3 positionWS, float3 grassUpWS, float3 windDir, float windStrength, 
    float vertexLocalHeight,  int instanceID)
{
    //计算草弯曲角度,0-90
    float rad = windStrength * PI * 0.9 / 2;
    //得到wind与grassUpWS的正交向量
    windDir = normalize(windDir - dot(windDir,grassUpWS) * grassUpWS);
    float x, y;  
    //x为单位球在wind方向，y为grassUp方向
    sincos(rad, x, y);
    //表示grassUpWS的顶点，会偏移到windedPos位置
    float3 windedPos = x * windDir + y * grassUpWS;

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

    //噪声做草微动
    float2 noiseUV = (positionWS.xz - _Time.y) / 20;
    float noiseValue = sin(tex2Dlod(_NoiseMap,float4(noiseUV,0,0)).r * _Wind.w);
    _Wind.w += noiseValue * _WindNoiseStrength;
    _Wind.w = saturate(_Wind.w);

    //计算草风吹后的新位置
    positionWS.xyz = ApplyWind(positionWS.xyz, grassUpDir, normalize(_Wind.xyz),
        _Wind.w, input.positionOS.y, input.instanceID);
    
    output.positionWS = positionWS;
    output.positionCS = TransformWorldToHClip(positionWS);
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.uv = input.uv;
    return output;
}

half4 LitPassFragment(Varyings input) : SV_Target
{
    half4 albedo = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv);
    clip(albedo.a - _Cutoff);
    
    half4 colorLerp = lerp(_BaseColor, _ColorTop, input.positionWS.y);
    albedo *= colorLerp;

    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
	float3 viewDirectionWS = normalize(_WorldSpaceCameraPos.xyz  - input.positionWS);
    Light mainLight = GetMainLight(shadowCoord);
	float3 mainAtten = mainLight.color * mainLight.distanceAttenuation;

    //Lambert
    float4 color = float4(1,1,1,1);
    color.rgb = max(0.2,abs(dot(mainLight.direction, input.normalWS))) * albedo.rgb * mainAtten;

    //TODO:草应该有高光
    
    //次表面散射
    mainAtten = lerp(mainAtten, mainAtten * mainLight.shadowAttenuation, _TransShadow);
    half3 mainLightDir = mainLight.direction + input.normalWS * _TransNormal;
    half mainLightVdotL = pow(saturate(dot(viewDirectionWS, -mainLightDir)), _TransScattering);
    half3 mainLightTranslucency = mainAtten * mainLightVdotL * _TransDirect;
    half3 scatteringColor = _ScatteringColor.rgb * albedo.rgb * mainLightTranslucency * _TransStrength;
    color.rgb += scatteringColor * lerp(0, 1, input.positionWS.y);
    
    return color;
}

#endif