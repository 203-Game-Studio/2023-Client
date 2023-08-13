Shader "John/VegetationTrunk"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)

		_BumpMap("Normal", 2D) = "bump" {}
		_NormalStrength("Normal Strength", Range( 0 , 1)) = 1

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.25
        _HidePower("Hide Power", Float) = 2.5

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

            #include "VegetationTrunk.hlsl"

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

            #include "VegetationShadowCaster.hlsl"
            ENDHLSL
        }

        Pass
        {
			//TODO:先用urp的DepthOnly 没查出来为什么树叶可以树干不行 有时间再找找bug
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            HLSLPROGRAM
            #pragma target 3.0
			
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
			
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }
}
