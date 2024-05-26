Shader "PostProcessing/Kuwahara"
{
	Properties
	{
		_MainTex("_MainTex", 2D) = "white" {}
		_BlurRange("Blur Range",Float) = 0.00015
	}

	HLSLINCLUDE
		#include "Kuwahara.hlsl"
	ENDHLSL

	SubShader{
		Tags { "RenderPipeline" = "UniversalPipeline" }
		ZTest Always
		ZWrite Off
		Cull Off

		Pass
		{
			Name "Kuwahara"
			HLSLPROGRAM
				#pragma vertex PostProcessingVert
				#pragma fragment KuwaharaFrag
			ENDHLSL
		}
	}
}