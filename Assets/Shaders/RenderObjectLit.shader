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
            #include "GPUDrivenDef.hlsl"

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

            StructuredBuffer<InstanceData> _InstanceDataBuffer;
            StructuredBuffer<uint2> _TriangleResult;
            StructuredBuffer<float3> _VerticesBuffer;
            StructuredBuffer<uint> _MeshletVerticesBuffer;
            StructuredBuffer<uint> _MeshletTrianglesBuffer;
            StructuredBuffer<Meshlet> _ClusterCullingResult;
            
            //Debug
            StructuredBuffer<float3> _DebugColorBuffer;
            uniform int _ColorCount;

            //uniform float4x4 _VPMatrix;

            void ConfigureProcedural(){}

            Varyings LitPassVertex (Attributes input)
            {
                Varyings output;
                uint2 id = _TriangleResult[input.insID];
                Meshlet meshlet = _ClusterCullingResult[id.y];
                uint index = _MeshletTrianglesBuffer[meshlet.triangleOffset + id.x * 3 + input.vertID];
                float3 vertex = _VerticesBuffer[_MeshletVerticesBuffer[meshlet.vertexOffset + index]];
                InstanceData insData = _InstanceDataBuffer[0];
                unity_ObjectToWorld = insData.objectToWorldMatrix;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(vertex);
                output.positionCS = positionInputs.positionCS;
                uint colorIdx = input.insID % _ColorCount;
                output.color = float4(_DebugColorBuffer[colorIdx], 1);
                //output.color = float4(saturate(meshlet.min + meshlet.max), 1);
                return output;
            }

            float4 LitPassFragment (Varyings input) : SV_Target
            {
                return input.color;
            }
            ENDHLSL
            
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5

            // -------------------------------------
            // Shader Stages
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "GPUDrivenDef.hlsl"
            #include "RenderObjectShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}