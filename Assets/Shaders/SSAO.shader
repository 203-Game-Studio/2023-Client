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
                #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
			ENDHLSL
		}

        Pass
        {
            Name "Horizontal Blur"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment HorizontalBlur
            ENDHLSL
        }

        Pass
        {
            Name "Vertical Blur"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment VerticalBlur
            ENDHLSL
        }

		Pass
		{
			Name "Final"
			HLSLPROGRAM
				#pragma vertex Vert
				#pragma fragment FinalFrag
			ENDHLSL
		}
	}
}
