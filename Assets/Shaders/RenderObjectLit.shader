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
            #pragma target 4.5
            #pragma enable_d3d11_debug_symbols
            #pragma multi_compile_instancing 
            #pragma instancing_options procedural:ConfigureProcedural
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

            struct Attributes
            {
                uint vertID : SV_VertexID;
                uint insID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : TEXCOOR0;
            };

            struct InstanceData
            {
                float4x4 objectToWorldMatrix;
                //something
            };

            struct Meshlet
            {
                uint vertexOffset;
                uint triangleOffset;
                uint vertexCount;
                uint triangleCount;
                //uint instanceOffset;
            };

            struct MeshletBounds
            {
                float3 min;
                float3 max;
                float3 coneApex;
                float3 coneAxis;
                float coneCutoff;
            };

            StructuredBuffer<InstanceData> _InstanceDataBuffer;
            StructuredBuffer<Meshlet> _MeshletBuffer;
            StructuredBuffer<MeshletBounds> _MeshletBoundsBuffer;
            StructuredBuffer<float3> _VerticesBuffer;
            StructuredBuffer<uint> _MeshletVerticesBuffer;
            StructuredBuffer<uint> _MeshletTrianglesBuffer;
            
            //Debug
            StructuredBuffer<float3> _DebugColorBuffer;
            uniform int _ColorCount;

            //uniform float4x4 _VPMatrix;

            void ConfigureProcedural(){}

            Varyings LitPassVertex (Attributes input)
            {
                Varyings output;
                Meshlet meshlet = _MeshletBuffer[input.insID];
                MeshletBounds meshletBounds = _MeshletBoundsBuffer[input.insID];
                uint index = _MeshletTrianglesBuffer[meshlet.triangleOffset + input.vertID];
                float3 vertex = _VerticesBuffer[_MeshletVerticesBuffer[meshlet.vertexOffset + index]];
                InstanceData insData = _InstanceDataBuffer[0];
                unity_ObjectToWorld = insData.objectToWorldMatrix;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(vertex);
                output.positionCS = positionInputs.positionCS;
                uint colorIdx = input.insID % _ColorCount;
                output.color = float4(_DebugColorBuffer[colorIdx], 1);
                //output.color = float4(saturate(meshletBounds.min + meshletBounds.max), 1);
                return output;
            }

            float4 LitPassFragment (Varyings input) : SV_Target
            {
                return input.color;
            }
            ENDHLSL
            
        }
    }
}