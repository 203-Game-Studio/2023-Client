Shader "John/Skybox"
{
    Properties
    {
		_SunColor("Sun Color", Color) = (1,1,1,1)
		_SunRadius("Sun Radius",  Range(0, 2)) = 0.1
		_MoonColor("Moon Color", Color) = (1,1,1,1)
		_MoonRadius("Moon Radius",  Range(0, 2)) = 0.1
    }

    SubShader
    {
        Tags {  
            "Queue" = "Background" 
            "RenderType" = "Background" 
            "RenderPipeline" = "UniversalPipeline" 
            "PreviewType" = "Skybox" 
        }
        Cull Off

        Pass
        {
            Cull off

            HLSLPROGRAM
            #pragma target 4.5

            #include "Skybox.hlsl"

            #pragma vertex SkyboxVertex
            #pragma fragment SkyboxFragment

            ENDHLSL
        }
    }
}
