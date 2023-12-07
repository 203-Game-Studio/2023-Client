Shader "John/ShallowWater"
{
    Properties
    {
    }

    SubShader
    {
        LOD 0

        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType"="Opaque" "IgnoreProjector" = "True"}

        Pass
        {
            Name "ForwardLit"
    		Tags { "LightMode"="UniversalForward" }

            Cull off

            HLSLPROGRAM
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_WaterHeightMap);
            SAMPLER(sampler_WaterHeightMap);

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output;

                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;

                return output;
            }

            half4 LitPassFragment(Varyings input) : SV_Target
            {
                half h = SAMPLE_TEXTURE2D(_WaterHeightMap, sampler_WaterHeightMap, input.uv);

                return half4(h, h, h, 1);
            }

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            ENDHLSL
        }
    }
}
