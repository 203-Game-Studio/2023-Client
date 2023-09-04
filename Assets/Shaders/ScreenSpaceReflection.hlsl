#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"

TEXTURE2D(_GTAOTexture); SAMPLER(sampler_GTAOTexture);
//这是世界空间的法线
float3 GetNormal(float2 uv){
    float4 cdn = SAMPLE_TEXTURE2D(_GTAOTexture, sampler_GTAOTexture,uv);
    //cdn.y = 1-cdn.y;
    float3 normal =  cdn.xyz* 2 - 1;
    return normal;
}

float4 _CameraDepthTexture_TexelSize;

// inspired by keijiro's depth inverse projection
// https://github.com/keijiro/DepthInverseProjection
// constructs view space ray at the far clip plane from the vpos
// then multiplies that ray by the linear 01 depth
float3 viewSpacePosAtPixelPosition(float2 vpos)
{
    float2 uv = vpos * _CameraDepthTexture_TexelSize.xy;
    float3 viewSpaceRay = mul(unity_CameraInvProjection, float4(uv * 2.0 - 1.0, 1.0, 1.0) * _ProjectionParams.z);
    float rawDepth = SampleSceneDepth(uv);
    return viewSpaceRay * Linear01Depth(rawDepth, _ZBufferParams);
}

TEXTURE2D(_SSRBuffer); SAMPLER(sampler_SSRBuffer);
float4 EfficentSSR(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.uv;
    uv.y = 1.0 - uv.y;
    float4 color = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, uv);
    float4 ssrBuffVal = SAMPLE_TEXTURE2D_X(_SSRBuffer, sampler_SSRBuffer, uv);
    
    half3 viewSpacePos_c = viewSpacePosAtPixelPosition(input.positionCS.xy + float2( 0.0, 0.0));

    // get view space position at 1 pixel offsets in each major direction
    half3 viewSpacePos_l = viewSpacePosAtPixelPosition(input.positionCS.xy + float2(-1.0, 0.0));
    half3 viewSpacePos_r = viewSpacePosAtPixelPosition(input.positionCS.xy + float2( 1.0, 0.0));
    half3 viewSpacePos_d = viewSpacePosAtPixelPosition(input.positionCS.xy + float2( 0.0,-1.0));
    half3 viewSpacePos_u = viewSpacePosAtPixelPosition(input.positionCS.xy + float2( 0.0, 1.0));

    // get the difference between the current and each offset position
    half3 l = viewSpacePos_c - viewSpacePos_l;
    half3 r = viewSpacePos_r - viewSpacePos_c;
    half3 d = viewSpacePos_c - viewSpacePos_d;
    half3 u = viewSpacePos_u - viewSpacePos_c;

    // pick horizontal and vertical diff with the smallest z difference
    half3 h = abs(l.z) < abs(r.z) ? l : r;
    half3 v = abs(d.z) < abs(u.z) ? d : u;

    // get view space normal from the cross product of the two smallest offsets
    half3 normalVS = normalize(cross(h, v));
        
    float2 texSize = ceil(_ScreenParams.xy);

    float depth = SampleSceneDepth(uv);

    uv.y = 1.0 - uv.y;
    float4 NdcPos = float4(uv * 2.0f - 1.0f, depth, 1.0f);
    NdcPos = mul(UNITY_MATRIX_I_P, NdcPos);
    NdcPos /= NdcPos.w;
    float3 viewPos = NdcPos.xyz;
    
    // In view space
    float3 viewDir = normalize(viewPos);
    // view transform don't have scale, so that V_IT = V
    float3 reflectDir = normalize(reflect(viewDir, normalVS));
    float3 reflectWS = TransformViewToWorldDir(reflectDir);
     float3 SH = SampleSH(reflectWS);

    float cosTheta = dot(-viewDir, normalVS);
    if (cosTheta <= 0.01) return float4(0, 0, 0, 0);

    float tanTheta = sqrt(1.0 - cosTheta * cosTheta) / cosTheta;
    float thickness = _Thickness*clamp(tanTheta, 0.0, 10.0);
    

    // Clip to the near plane
    float rayLength = (_ProjectionParams.x*(viewPos.z + reflectDir.z * _MaxDistance) < _ProjectionParams.y)
                          ? (_ProjectionParams.y - _ProjectionParams.x*viewPos.z) / reflectDir.z*_ProjectionParams.x
                          : _MaxDistance;

    float4 startView = float4(viewPos, 1.0);
    float4 endView = float4(viewPos + (reflectDir * rayLength), 1.0);

    float4 startFrag = mul(UNITY_MATRIX_P, startView);
    startFrag = startFrag / startFrag.w;
    startFrag.xy = startFrag.xy * 0.5 + 0.5;
    startFrag.y = 1.0 - startFrag.y;
    startFrag.xy = startFrag.xy * texSize;
    
    float4 endFrag = mul(UNITY_MATRIX_P, endView);
    endFrag = endFrag / endFrag.w;
    endFrag.xy = endFrag.xy * 0.5 + 0.5;
    endFrag.y = 1.0 - endFrag.y;
    endFrag.xy = endFrag.xy * texSize;

    float deltaX = endFrag.x - startFrag.x;
    float deltaY = endFrag.y - startFrag.y;

    float useX = abs(deltaX) >= abs(deltaY) ? 1.0 : 0.0;
    float delta = lerp(abs(deltaY), abs(deltaX), useX)*_ReflectionStride;
    float2 increment = float2(deltaX, deltaY) / max(delta, 0.001);

    
    float i = 0;
    float search0 = 0;
    float search1 = 0;

    int hit0 = 0;
    int hit1 = 0;
    
    float2 frag = startFrag;
    frag += increment * _ReflectionJitter;

    UNITY_LOOP
    for (i = 0; i < int(delta); ++i)
    {
        frag += increment;
        if(frag.x < 0.0 || frag.y < 0.0 || frag.x > texSize.x || frag.y > texSize.y) break;
        float2 fragUV = frag / texSize;
        
        float fragDepth = LinearEyeDepth(SampleSceneDepth(fragUV), _ZBufferParams);
        
        search1 = lerp((frag.y - startFrag.y) / deltaY, (frag.x - startFrag.x) / deltaX, useX);
        search1 = clamp(search1, 0.0, 1.0);
    
        // unity's view space depth is negative
        float viewDepth = _ProjectionParams.x* (startView.z * endView.z) / lerp(endView.z, startView.z, search1);
        float deltaDepth = viewDepth - fragDepth;
    
        if (deltaDepth > 0 && deltaDepth < thickness)
        {
            hit0 = 1;
            break;
        }
        search0 = search1;
    }

    search1 = search0 + ((search1 - search0) / 2.0);
    
    float steps = _MaxSteps * hit0;
    UNITY_LOOP
    for (i = 0; i < steps; ++i)
    {
        frag = lerp(startFrag.xy, endFrag.xy, search1);
        if(frag.x < 0.0 || frag.y < 0.0 || frag.x > texSize.x || frag.y > texSize.y) break;
        
        float2 fragUV = frag / texSize;
        float fragDepth = LinearEyeDepth(SampleSceneDepth(fragUV), _ZBufferParams);
    
        float viewDepth = _ProjectionParams.x*(startView.z * endView.z) / lerp(endView.z, startView.z, search1);
        float deltaDepth = viewDepth - fragDepth;
    
        if (deltaDepth > 0 && deltaDepth < thickness*0.1)
        {
            hit1 = 1;
            search1 = search0 + ((search1 - search0) / 2);
        }
        else
        {
            float temp = search1;
            search1 = search1 + ((search1 - search0) / 2);
            search0 = temp;
        }
    }
    
    float4 reflColor = 0;//float4(SH,1);
    if(hit1 == 1)
    {
        float2 fragUV = frag / texSize;
        reflColor = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, fragUV);
    }

    half ssrSmoothness = ssrBuffVal.r;
    ssrSmoothness = saturate(ssrBuffVal.r);
    reflColor.rgb = lerp(0,reflColor.rgb,ssrSmoothness);

    return float4(reflColor.rgb, 1.0f);
}

