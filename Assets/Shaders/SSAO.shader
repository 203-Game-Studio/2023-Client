Shader "John/SSAO"
{
	HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
		#include "SSAO.hlsl"
	ENDHLSL

	SubShader{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
		ZTest Always
		ZWrite Off
		Cull Off

		Pass
		{
			Name "SSAO"
			HLSLPROGRAM
				#pragma vertex Vert
				#pragma fragment SSAOFrag
			ENDHLSL
		}
	}
}
