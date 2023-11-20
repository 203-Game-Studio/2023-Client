#ifndef _QUARK_BLUR_
#define _QUARK_BLUR_

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

half4 SSAOFrag(Varyings input) : SV_Target
{
    float3 normal = SampleSceneNormals(input.texcoord);
    //float3 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
    return half4(normal, 1);
}

#endif