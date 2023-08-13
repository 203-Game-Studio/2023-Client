Shader "John/VegetationLeaves"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.25
        _HidePower("Hide Power", Float) = 2.5

        _GradientColor("Gradient Color", Color) = (1,1,1,0)
		_GradientFalloff("Gradient Falloff", Range( 0 , 2)) = 2
		_GradientPosition("Gradient Position", Range( 0 , 1)) = 0.5

		_ColorVariationNoise("Color Variation Noise", 2D) = "white" {}
		_NoiseScale("Noise Scale", Float) = 0.5
        _ColorVariation("Color Variation", Color) = (1,0,0,0)
		_ColorVariationPower("Color Variation Power", Range( 0 , 1)) = 1

        _TransStrength("Strength", Range( 0, 50 ) ) = 1
		_TransNormal("Normal Distortion", Range( 0, 1 ) ) = 0.5
		_TransScattering("Scattering", Range( 1, 50 ) ) = 2
		_TransDirect("Direct", Range( 0, 1 ) ) = 0.9
		_TransAmbient("Ambient", Range( 0, 1 ) ) = 0.1
		_TransShadow("Shadow", Range( 0, 1 ) ) = 0.5

        _WindMultiplier("BaseWind Multiplier", Float) = 0
		_MicroWindMultiplier("MicroWind Multiplier", Float) = 1
		_WindTrunkPosition("Wind Trunk Position", Float) = 0
		_WindTrunkContrast("Wind Trunk Contrast", Float) = 10
    }

    SubShader
    {
        LOD 0

        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
    		Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _SHADOWS_SOFT

            #include "Vegetation.hlsl"

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #define VEGETATION_LEAVES

            #include "VegetationShadowCaster.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #define VEGETATION_LEAVES

            #include "VegetationShadowCaster.hlsl"
            ENDHLSL
        }
    }
}
