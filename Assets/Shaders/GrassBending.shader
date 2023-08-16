Shader "John/Grass"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        LOD 0

        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType"="Opaque" "IgnoreProjector" = "True"}

        Pass
        {
            Name "ForwardLit"
    		Tags {"LightMode"="UniversalForward"}

            HLSLPROGRAM
            #pragma target 3.0
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4  _BaseColor;
            CBUFFER_END

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

            Varyings vert (Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = input.uv;

                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                half4 col = _BaseColor.rgba;
                half2 xy = input.uv - 0.5;  // -0.5 ~ 0.5
                half len = dot(xy, xy);
                if (len < 0.25)
                {
                    col.b = clamp(sqrt(len), 0.0001, 0.5);
                    col.rg = xy / col.b;
                    col.b = (0.5 - col.b) * 2; //0 ~ 1
                }

                return col;
            }

            #pragma vertex vert
            #pragma fragment frag

            ENDHLSL
        }
    }
}
