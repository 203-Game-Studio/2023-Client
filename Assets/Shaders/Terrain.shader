Shader "John/Terrain"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _HeightMap ("Texture", 2D) = "white" {}
        _NormalMap ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "LightMode" = "UniversalForward"}
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct RenderPatch{
                float2 position;
                float2 minMaxHeight;
                uint lod;
                uint4 lodTrans;
            };
            StructuredBuffer<RenderPatch> PatchList;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _NormalMap;
            sampler2D _HeightMap;
            uniform float3 _WorldSize;
            float4x4 _WorldToNormalMapMatrix;

            float3 TransformNormalToWorldSpace(float3 normal){
                return SafeNormalize(mul(normal,(float3x3)_WorldToNormalMapMatrix));
            }


            float3 SampleNormal(float2 uv){
                float3 normal;
                normal.xz = tex2Dlod(_NormalMap, float4(uv, 0, 0)).xy * 2 - 1;
                normal.y = sqrt(max(0, 1 - dot(normal.xz, normal.xz)));
                normal = TransformNormalToWorldSpace(normal);
                return normal;
            }

            //Patch为16x16的Mesh
            #define PATCH_MESH_GRID_COUNT 16
            //Node为8x8个Patch
            #define PATCH_MESH_SIZE 8
            //Patch格子大小为0.5x0.5
            #define PATCH_MESH_GRID_SIZE 0.5
            //修复接缝
            void FixLODConnectSeam(inout float4 vertex, inout float2 uv, RenderPatch patch){
                uint4 lodTrans = patch.lodTrans;
                uint2 vertexIndex = floor((vertex.xz + PATCH_MESH_SIZE * 0.5 + 0.01) / PATCH_MESH_GRID_SIZE);
                float uvGridStrip = 1.0/PATCH_MESH_GRID_COUNT;

                uint lodDelta = lodTrans.x;
                if(lodDelta > 0 && vertexIndex.x == 0){
                    uint gridStripCount = pow(2,lodDelta);
                    uint modIndex = vertexIndex.y % gridStripCount;
                    if(modIndex != 0){
                        vertex.z -= PATCH_MESH_GRID_SIZE * modIndex;
                        uv.y -= uvGridStrip * modIndex;
                        return;
                    }
                }

                lodDelta = lodTrans.y;
                if(lodDelta > 0 && vertexIndex.y == 0){
                    uint gridStripCount = pow(2,lodDelta);
                    uint modIndex = vertexIndex.x % gridStripCount;
                    if(modIndex != 0){
                        vertex.x -= PATCH_MESH_GRID_SIZE * modIndex;
                        uv.x -= uvGridStrip * modIndex;
                        return;
                    }
                }

                lodDelta = lodTrans.z;
                if(lodDelta > 0 && vertexIndex.x == PATCH_MESH_GRID_COUNT){
                    uint gridStripCount = pow(2,lodDelta);
                    uint modIndex = vertexIndex.y % gridStripCount;
                    if(modIndex != 0){
                        vertex.z += PATCH_MESH_GRID_SIZE * (gridStripCount - modIndex);
                        uv.y += uvGridStrip * (gridStripCount- modIndex);
                        return;
                    }
                }

                lodDelta = lodTrans.w;
                if(lodDelta > 0 && vertexIndex.y == PATCH_MESH_GRID_COUNT){
                    uint gridStripCount = pow(2,lodDelta);
                    uint modIndex = vertexIndex.x % gridStripCount;
                    if(modIndex != 0){
                        vertex.x += PATCH_MESH_GRID_SIZE * (gridStripCount- modIndex);
                        uv.x += uvGridStrip * (gridStripCount- modIndex);
                        return;
                    }
                }
            }

            v2f vert (appdata input)
            {
                v2f output;

                RenderPatch patch = PatchList[input.instanceID];
                FixLODConnectSeam(input.vertex, input.uv, patch);
                uint lod = patch.lod;
                float scale = pow(2,lod);
                input.vertex.xz *= scale;
                input.vertex.xz += patch.position;

                float2 heightUV = (input.vertex.xz + (_WorldSize.xz * 0.5) + 0.5) / (_WorldSize.xz + 1);
                float height = tex2Dlod(_HeightMap, float4(heightUV,0,0)).r;
                input.vertex.y = height * _WorldSize.y;

                float3 normal = SampleNormal(heightUV);
                Light light = GetMainLight();
                output.color = max(0.05,dot(light.direction, normal));

                output.vertex = TransformObjectToHClip(input.vertex.xyz);
                output.uv = input.uv;
                //output.color = lerp(float4(1,1,1,1),float4(0,0,0,1),lod/5.0);

                return output;
            }

            half4 frag (v2f input) : SV_Target
            {
                return input.color;
            }
            ENDHLSL
        }
    }
}