#ifndef SKYBOX_INCLUDED
#define SKYBOX_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
	float3 uv         : TEXCOORD0;
};
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 uv         : TEXCOORD0;
};

float _SunRadius;
float4 _SunColor;

Varyings SkyboxVertex(Attributes input)
{
    Varyings output;
    
    output.uv = input.uv;
    output.positionCS = TransformObjectToHClip(input.positionOS);

    return output;
}

half4 SkyboxFragment(Varyings input) : SV_Target
{
    // sun
    float sun = distance(input.uv.xyz, _MainLightPosition);
    float sunDisc = 1 - (sun / _SunRadius);
	sunDisc = saturate(sunDisc * 50);
	float3 sunAndMoon = (sunDisc * _SunColor);
    return half4(sunAndMoon, 1);
}

#endif