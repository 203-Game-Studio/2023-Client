Shader "John/Grass"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("草根颜色", Color) = (1,1,1,1)
        _ColorTop("草尖颜色", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _NoiseMap("草浪噪声图", 2D) = "white" {}
        _WindNoiseStrength("草浪强度",Range(0,20)) = 10
        _Wind("Wind(xyz方向,w强度)",Vector) = (1,0,0,10)
        
        _ScatteringColor("Scattering Color", Color) = (1,1,1,1)
        _TransStrength("Strength", Range( 0, 50 ) ) = 1
		_TransNormal("Normal Distortion", Range( 0, 1 ) ) = 0.5
		_TransScattering("Scattering", Range( 1, 50 ) ) = 2
		_TransDirect("Direct", Range( 0, 1 ) ) = 0.9
		_TransShadow("Shadow", Range( 0, 1 ) ) = 0.5
    }

    SubShader
    {
        LOD 0

        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType"="Opaque" "IgnoreProjector" = "True"}

        Pass
        {
            Name "ForwardLit"
    		Tags { "LightMode"="UniversalForward" }

            Cull off

            HLSLPROGRAM
            #pragma target 3.0

            #pragma shader_feature _RECEIVE_SHADOWS_OFF
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup

            #include "Grass.hlsl"

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            ENDHLSL
        }
    }
}
