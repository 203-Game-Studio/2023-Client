#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

//解码法线贴图函数

inline float3 DecodeViewNormalStereo(float4 enc4)
{
    float kScale = 1.7777;
    float3 nn = enc4.xyz * float3(2 * kScale, 2 * kScale, 0) + float3(-kScale, -kScale, 1);
    float g = 2.0 / dot(nn.xyz, nn.xyz);
    float3 n;
    n.xy = g * nn.xy;
    n.z = g - 1;
    return n;
}

float4 EfficentSSR(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.uv;
    uv.y = 1.0 - uv.y;
    float4 color = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, uv);
    
    float3 normalVS = DecodeViewNormalStereo(
        SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, uv));
        
    float2 texSize = ceil(_ScreenParams.xy);

    // Get camera space position
#if UNITY_REVERSED_Z
    float depth = SampleSceneDepth(uv);
#else
    // Adjust z to match OpenGL's NDC
    float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
#endif

    uv.y = 1.0 - uv.y;
    float4 NdcPos = float4(uv * 2.0f - 1.0f, depth, 1.0f);
    NdcPos = mul(UNITY_MATRIX_I_P, NdcPos);
    NdcPos /= NdcPos.w;
    float3 viewPos = NdcPos.xyz;
    
    // In view space
    float3 viewDir = normalize(viewPos);
    // view transform don't have scale, so that V_IT = V
    float3 reflectDir = normalize(reflect(viewDir, normalVS));

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
    frag += increment*_ReflectionJitter;

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
    
        if (deltaDepth>0&&deltaDepth < thickness)
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
    
    float4 reflColor = 0;
    if(hit1 ==1 )
    {
        float2 fragUV = frag / texSize;
        reflColor = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, fragUV);
    }

    return float4(reflColor.rgb, 1.0f);
}

