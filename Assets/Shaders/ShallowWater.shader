Shader "John/ShallowWater"
{
    Properties
    {
        //[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        _ShallowWaterColor("Shallow Water Color", Color) = (1, 1, 1, 1)
        _DeepWaterColor("Deep Water Color", Color) = (1, 1, 1, 1)
        _DepthDistancePower("Depth Distance Power", Range(0, 1)) = 0.5

		_WaveNormalMap1("Wave Normal Map 1", 2D) = "bump"{}
		_WaveNormalScale1("Wave Normal Scale", Range(0.1, 1)) = 1
		_WaveNormalMap2("Wave Normal Map 2", 2D) = "bump"{}
		_WaveNormalScale2("Wave Normal Scale", Range(0.1, 1)) = 1

		_WaveXSpeed("Wave X Speed", Range(-0.1, 0.1)) = 0.01
		_WaveYSpeed("Wave Y Speed", Range(-0.1, 0.1)) = 0.01

        _SkyBox("SkyBox", Cube) = "white"{}
        _SkyBoxReflectSmooth("SkyBox Reflect Smooth", Range(1, 5)) = 3
        _RefractionPower("Refraction Power", Range(0, 0.1)) = 0.05

        _Gloss("Gloss", Float) = 10.0
        _Shininess("Shininess", Float) = 200
        _FresnelPower("Fresnel Power", Range(1, 5)) = 3

        _FoamTex("Foam Map", 2D) = "white" {}
        _FoamPower("Foam Power", Range(0, 1)) = 0.5

        _CausticsTex("Caustics Map", 2D) = "white" {}
        _CausticsPower("Caustics Power", Range(0, 1)) = 0.5
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

            TEXTURE2D(_WaterHeightMap);
            SAMPLER(sampler_WaterHeightMap);

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output;

                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                //output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.uv = input.uv;

                return output;
            }

            half4 LitPassFragment(Varyings input) : SV_Target
            {
                float2 speed = _Time.y * float2(_WaveXSpeed, _WaveYSpeed);
                float3 waveNormal1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_WaveNormalMap1, sampler_WaveNormalMap1, input.uv + speed),_WaveNormalScale1).xzy;
                float3 waveNormal2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_WaveNormalMap2, sampler_WaveNormalMap2, input.uv - speed),_WaveNormalScale2).xzy;
                float3 waveBlendNormal = normalize(waveNormal1 + waveNormal2);

                float2 texGridSizeX = float2(1/1024.0, 0);
                float2 texGridSizeY = float2(0, 1/1024.0);
                float waterHeight = SAMPLE_TEXTURE2D(_WaterHeightMap, sampler_WaterHeightMap, input.uv);
                float waterHeightX1 = SAMPLE_TEXTURE2D(_WaterHeightMap, sampler_WaterHeightMap, input.uv - texGridSizeX);
                float waterHeightX2 = SAMPLE_TEXTURE2D(_WaterHeightMap, sampler_WaterHeightMap, input.uv + texGridSizeX);
                float waterHeightY1 = SAMPLE_TEXTURE2D(_WaterHeightMap, sampler_WaterHeightMap, input.uv - texGridSizeY);
                float waterHeightY2 = SAMPLE_TEXTURE2D(_WaterHeightMap, sampler_WaterHeightMap, input.uv + texGridSizeY);
                float heightX = (waterHeightX2 - waterHeightX1) * 0.5;
                float heightY = (waterHeightY2 - waterHeightY1) * 0.5;
                float3 heightNormal = float3(heightX*10, waterHeight, heightY*10);
                waveBlendNormal = normalize(heightNormal + waveBlendNormal);

                float3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
                
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                
                half3 specularColor = WaterSpecular(viewDir, mainLight.direction, mainLight.color, waveBlendNormal);
                specularColor += GetSkyBoxColor(viewDir, waveBlendNormal);

                float2 screenUV = input.positionCS.xy * (_ScreenParams.zw - 1);
                half3 refractionColor = GetRefractionColor(screenUV, waveBlendNormal);

                float reflectionCoefficient = GetReflectionCoefficient(viewDir, waveBlendNormal);
                half3 color = lerp(refractionColor, specularColor, reflectionCoefficient);
                color += SampleSH(waveBlendNormal) * 0.1;

                float depth = SampleSceneDepth(screenUV);
                float distanceWS = GetPosDistanceWS(input.positionWS, screenUV, depth);
                float foamStrength = GetFoamStrength(distanceWS);
                float2 foamUV = (input.uv + _Time.y * float2(0.01,0.01) + waveBlendNormal.xz * 0.005) * 30;
                half3 foamColor = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, foamUV);

                float depthCoefficient = pow(saturate((distanceWS * _DepthDistancePower)), 0.2);
                half3 waterBaseColor = lerp(_ShallowWaterColor, _DeepWaterColor, depthCoefficient);
                half3 diffuseColor = WaterDiffuse(mainLight.shadowAttenuation, mainLight.direction, mainLight.color, waveBlendNormal) * waterBaseColor;
                color += diffuseColor;

                half3 causticsColor = GetCausticsColor(screenUV, depth, waveBlendNormal);
                color += causticsColor * _CausticsPower;

                color = lerp(color, foamColor, foamStrength);

                return half4(color, 1);
            }

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            ENDHLSL
        }
    }
}
