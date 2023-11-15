#ifndef SPRITES_FORWARD_INCLUDED
#define SPRITES_FORWARD_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#ifdef UNITY_INSTANCING_ENABLED

    UNITY_INSTANCING_BUFFER_START(PerDrawSprite)
        // SpriteRenderer.Color while Non-Batched/Instanced.
        UNITY_DEFINE_INSTANCED_PROP(half4, unity_SpriteRendererColorArray)
        // this could be smaller but that's how bit each entry is regardless of type
        UNITY_DEFINE_INSTANCED_PROP(half2, unity_SpriteFlipArray)
    UNITY_INSTANCING_BUFFER_END(PerDrawSprite)

    #define _RendererColor  UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteRendererColorArray)
    #define _Flip           UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteFlipArray)

#endif

CBUFFER_START(UnityPerDrawSprite)
#ifndef UNITY_INSTANCING_ENABLED
    half4 _RendererColor;
    half2 _Flip;
#endif
    float _EnableExternalAlpha;
CBUFFER_END

half4 _Color;

struct Attributes
{
    float4 vertex   : POSITION;
    float4 color    : COLOR;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    half4 color    : COLOR;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

inline float4 UnityFlipSprite(in float3 pos, in half2 flip)
{
    return float4(pos.xy * flip, pos.z, 1.0);
}

// snaps post-transformed position to screen pixels
inline float4 UnityPixelSnap (float4 pos)
{
    float2 hpc = _ScreenParams.xy * 0.5f;
#if  SHADER_API_PSSL
// An old sdk used to implement round() as floor(x+0.5) current sdks use the round to even method 
//so we manually use the old method here for compatabilty.
    float2 temp = ((pos.xy / pos.w) * hpc) + float2(0.5f,0.5f);
    float2 pixelPos = float2(floor(temp.x), floor(temp.y));
#else
    float2 pixelPos = round ((pos.xy / pos.w) * hpc);
#endif
    pos.xy = pixelPos / hpc * pos.w;
    return pos;
}

Varyings SpriteVert(Attributes input)
{
    Varyings output;

    UNITY_SETUP_INSTANCE_ID (input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.positionCS = TransformObjectToHClip(UnityFlipSprite(input.vertex, _Flip));
    output.uv = input.texcoord;
    output.color = input.color * _Color * _RendererColor;

    #ifdef PIXELSNAP_ON
    output.positionCS = UnityPixelSnap (output.positionCS);
    #endif

    return output;
}

sampler2D _MainTex;

half4 SpriteFrag(Varyings input) : SV_Target
{
    half4 color = tex2D(_MainTex, input.uv) * input.color;
    color.rgb *= color.a;
    return color;
}

#endif