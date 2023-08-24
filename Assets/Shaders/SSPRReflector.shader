// SSPRReflector.shader
Shader "My Shader/SSPRReflector" {
    Properties {}
    SubShader {
        Tags {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Overlay"
        }
        Pass {
            Name "SSPR Reflector Pass"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "SSPRReflectorPass.hlsl"

            #pragma vertex SSPRReflectorPassVertex
            #pragma fragment SSPRReflectorPassFragment
            ENDHLSL
        }
    }
}