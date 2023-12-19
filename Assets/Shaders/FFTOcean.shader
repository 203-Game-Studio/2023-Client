Shader "John/FFTOcean"
{
    Properties
    {
    }

    SubShader
    {
        LOD 0

        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType"="Transparent" "IgnoreProjector" = "True"}

        Pass
        {
            Name "ForwardLit"
    		Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "WaterCommon.hlsl"

            TEXTURE2D(_HeightSpectrumRT);
            SAMPLER(sampler_HeightSpectrumRT);

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output;

                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;

                return output;
            }

            half4 LitPassFragment(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_HeightSpectrumRT, sampler_HeightSpectrumRT, input.uv);
                return half4(color.rgb, 1);
            }

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            ENDHLSL
        }
    }
}
