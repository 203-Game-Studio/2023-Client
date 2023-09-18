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
        Tags { "RenderType"="Opaque" "LightMode" = "UniversalForward"}
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
                uint lod;
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

            v2f vert (appdata v)
            {
                v2f o;

                RenderPatch patch = PatchList[v.instanceID];
                uint lod = patch.lod;
                float scale = pow(2,lod);
                v.vertex.xz *= scale;
                v.vertex.xz += patch.position;

                float2 heightUV = (v.vertex.xz + (_WorldSize.xz * 0.5) + 0.5) / (_WorldSize.xz + 1);
                float height = tex2Dlod(_HeightMap, float4(heightUV,0,0)).r;
                v.vertex.y = height * _WorldSize.y;

                float3 normal = SampleNormal(heightUV);
                Light light = GetMainLight();
                o.color = max(0.05,dot(light.direction,normal));

                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                //o.color = lerp(float4(1,1,1,1),float4(0,0,0,1),lod/5.0);

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDHLSL
        }
    }
}