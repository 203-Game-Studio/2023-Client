Shader "John/FFTOcean"
{
    Properties
    {
        //[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        _ShallowWaterColor("Shallow Water Color", Color) = (1, 1, 1, 1)
        _DeepWaterColor("Deep Water Color", Color) = (1, 1, 1, 1)
        _DepthDistancePower("Depth Distance Power", Range(0, 1)) = 0.5

        _SkyBox("SkyBox", Cube) = "white"{}
        _SkyBoxReflectSmooth("SkyBox Reflect Smooth", Range(1, 5)) = 3
        _RefractionPower("Refraction Power", Range(0, 0.1)) = 0.05

        _Gloss("Gloss", Float) = 10.0
        _Shininess("Shininess", Float) = 200
        _FresnelPower("Fresnel Power", Range(1, 5)) = 3
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

            TEXTURE2D(_DisplaceMap);
            SAMPLER(sampler_DisplaceMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output;

                float4 displcae = SAMPLE_TEXTURE2D_LOD(_DisplaceMap, sampler_DisplaceMap, float4(input.uv, 0.0, 0.0), 0.0);
                input.positionOS += float4(displcae.xyz, 0);
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.positionOS);
                output.uv = input.uv;

                return output;
            }

            half4 LitPassFragment(Varyings input) : SV_Target
            {
                float3 waveBlendNormal = TransformObjectToWorldNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv).rgb);

                float3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
                
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                
                half3 specularColor = WaterSpecular(viewDir, mainLight.direction, mainLight.color, waveBlendNormal);
                specularColor += GetSkyBoxColor(viewDir, waveBlendNormal);

                float2 screenUV = input.positionCS.xy * (_ScreenParams.zw - 1);
                //half3 refractionColor = GetRefractionColor(screenUV, waveBlendNormal);

                //float reflectionCoefficient = GetReflectionCoefficient(viewDir, waveBlendNormal);
                //half3 color = lerp(refractionColor, specularColor, reflectionCoefficient);
                half3 color = specularColor + SampleSH(waveBlendNormal) * 0.1;

                float depth = SampleSceneDepth(screenUV);
                float distanceWS = GetPosDistanceWS(input.positionWS, screenUV, depth);

                float depthCoefficient = pow(saturate((distanceWS * _DepthDistancePower)), 0.2);
                half3 waterBaseColor = lerp(_ShallowWaterColor, _DeepWaterColor, depthCoefficient);
                half3 diffuseColor = WaterDiffuse(mainLight.shadowAttenuation, mainLight.direction, mainLight.color, waveBlendNormal) * waterBaseColor;
                color += diffuseColor;

                return half4(color, 1);
            }

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            ENDHLSL
        }
    }
}
