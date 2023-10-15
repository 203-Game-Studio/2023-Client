Shader "John/RenderObjectLit"
{
    Properties
    {
    }
    SubShader
    {
        Tags{"RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True" "ShaderModel"="4.5" "Queue" = "Transparent"}
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}

            HLSLPROGRAM
            #pragma instancing_options procedural:ConfigureProcedural
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma multi_compile_instancing 
            

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

            struct Meshlet
            {
                uint vertexOffset;
                uint triangleOffset;
                uint vertexCount;
                uint triangleCount;
            };

            struct appdata
            {
                uint vertID : SV_VertexID;
                uint insID : SV_InstanceID;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
            };

            StructuredBuffer<float3> _VerticesBuffer;
            StructuredBuffer<Meshlet> _MeshletBuffer;
            StructuredBuffer<uint> _MeshletVerticesBuffer;
            StructuredBuffer<uint> _MeshletTrianglesBuffer;
            

            void ConfigureProcedural(){}

            v2f vert (appdata input)
            {
                v2f output;
                Meshlet meshlet = _MeshletBuffer[input.insID];
                uint index = 0;
                if(input.vertID < meshlet.triangleCount){
                    _MeshletTrianglesBuffer[meshlet.triangleOffset + input.vertID*3];
                }
                float3 v = _VerticesBuffer[_MeshletVerticesBuffer[meshlet.vertexOffset + index]];
                output.positionCS = mul(UNITY_MATRIX_VP, float4(v, 1));
                return output;
            }

            float4 frag (v2f input) : SV_Target
            {
                return float4(1,1,1,1);
            }
            ENDHLSL
            
        }
    }
}