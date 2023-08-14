Shader "John/Grass"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        _DiffuseColorLow("草根颜色", Color) = (1,1,1,1)
        _DiffuseColorMid("草中段颜色", Color) = (1,1,1,1)
        _DiffuseColorHigh("草尖颜色", Color) = (1,1,1,1)
        _SpecularColor("高光颜色", Color) = (1,1,1,1)
        _Roughness("草光滑度", Range(0,1)) = 1
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        
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
