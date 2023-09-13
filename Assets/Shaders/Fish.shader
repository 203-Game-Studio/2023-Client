Shader "Unlit/Fish"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        
        _SwimSpeed("游动速度", Range(0, 20)) = 10
        _MoveOffset("摆动位移", Range(0, 2)) = 0.05
        _HorRotate("左右旋转幅度", Range(0, 2)) = 0.02
        _VerRotate("上下旋转幅度", Range(0, 2)) = 0.1
        _SwimMask("游动遮罩", Range(0, 50)) = 0.5
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

            struct appdata
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normal     : NORMAL;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 normal     : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _BaseColor;
                float _SwimSpeed;
                float _MoveOffset;
                float _HorRotate;
                float _VerRotate;
                float _SwimMask;
            CBUFFER_END

            TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);


            struct FishData{
                float3 position;
                float3 direction;
            };
            #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                StructuredBuffer<FishData> _Fishes;
            #endif
            

            void ConfigureProcedural(){}

            float3x3 AngleAxis3x3(float angle, float3 axis)
	        {
	        	float c, s;
	        	sincos(angle, s, c);
	        	float t = 1 - c;
	        	float x = axis.x;
	        	float y = axis.y;
	        	float z = axis.z;
	        	return float3x3(t * x * x + c, t * x * y - s * z, t * x * z + s * y,
	        		t * x * y + s * z, t * y * y + c, t * y * z - s * x,
	        		t * x * z - s * y, t * y * z + s * x, t * z * z + c);
	        }

            float3 FishAnim(float3 positionOS){
                float sinTime = sin(_Time.y * _SwimSpeed);

                float offset = sinTime * _MoveOffset;
                float offset2 = sin((positionOS.x)/0.3+_Time.y * _SwimSpeed) * _MoveOffset;

                float3x3 rotMat = AngleAxis3x3(sinTime * _HorRotate, float3(0,1,0));
                float3x3 roaMat2=AngleAxis3x3(sin(_Time.y * _SwimSpeed + positionOS.x) * _VerRotate, float3(1,0,0));

                float mask = smoothstep(positionOS.x, positionOS.x + _SwimMask, 1);

                positionOS = lerp(positionOS, mul(roaMat2,mul(rotMat, positionOS)), mask);
                positionOS.z += lerp(0, offset2, mask) - offset;

                return positionOS;
            }

            v2f vert (appdata input, uint instanceID : SV_InstanceID)
            {
                v2f output;

                input.positionOS.xyz = FishAnim(input.positionOS.xyz);
                
                #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                    FishData fish = _Fishes[instanceID];
                    unity_ObjectToWorld._m03_m13_m23_m33 = float4(fish.position, 1.0f);
                    float3 up = float3(0, 1, 0);
                    float3 forward = normalize(fish.direction);
                    float3 right = normalize(cross(up, forward));
                    up = cross(forward, right);
                    unity_ObjectToWorld._m00_m10_m20 = right;
                    unity_ObjectToWorld._m01_m11_m21 = up;
                    unity_ObjectToWorld._m02_m12_m22 = forward;
                    //float4x4 rot = float4x4(float4(right,0), float4(up2,0), float4(forward,0), float4(0,0,0,1));
                    //unity_ObjectToWorld = mul(rot, unity_ObjectToWorld);
                    output.positionCS = TransformObjectToHClip(input.positionOS);
                #else   
                    output.positionCS = TransformObjectToHClip(input.positionOS);
			    #endif

                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            float4 frag (v2f input) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return col;
            }
            ENDHLSL
            
        }
    }
}