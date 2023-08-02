Shader "203/Voxel"
{
    Properties
    {
        _BaseColor("BaseColor", Color) = (1.0, 1.0, 1.0, 1.0)
    }

        SubShader
        {
            Tags
            {
                "RanderPipline" = "UniversalPipeline"
                "RanderType" = "Opaque"
            }

            HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS   : TEXCOORD1;
            };

            ENDHLSL

            Pass
            {
                Name "ForwardUnlit"
                Tags {"LightMode" = "UniversalForward"}
            
                HLSLPROGRAM
            
                #pragma vertex vert
                #pragma fragment frag
            
                Varyings vert(Attributes input)
                {
                    const VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                    const VertexNormalInputs   vertexNormalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
            
                    Varyings output;
                    output.positionCS = vertexInput.positionCS;
                    output.normalWS = vertexNormalInput.normalWS;
                    return output;
                }
            
                half4 frag(Varyings input) : SV_Target
                {
                    Light mainLight = GetMainLight();
                    real4 lightColor = real4(mainLight.color,1);
                    real3 normalWS = normalize(input.normalWS);
                    real3 lightDir = normalize(mainLight.direction); 
                    real lightAten = saturate(dot(lightDir,normalWS));
                    return lightAten * lightColor;
                }
                ENDHLSL
            }
        }
}