using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Kuwahara : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Range(1.0f, 10.0f)]public float radius = 3.0f;
    }
    public Settings settings;

    KuwaharaPass kuwaharaPass;

    public override void Create()
    {
        kuwaharaPass = new KuwaharaPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        kuwaharaPass.Setup(settings);
        renderer.EnqueuePass(kuwaharaPass);
    }

    public class KuwaharaPass : ScriptableRenderPass
    {
        static readonly int destId = Shader.PropertyToID("_TempTargetGaussian");

        private Material kuwaharaMaterial;

        private RenderTargetIdentifier src;
        private Kuwahara.Settings settings;

        static readonly string cmdName = "Kuwahara";
        private CommandBuffer cmd;

        public void Setup(Kuwahara.Settings settings)
        {
            this.settings = settings;
        }

        public KuwaharaPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            Shader kuwaharaShader = Shader.Find("PostProcessing/Kuwahara");
            if (kuwaharaShader is null)
            {
                Debug.LogError("Kuwahara shader not found.");
                return;
            }
            kuwaharaMaterial = CoreUtils.CreateEngineMaterial(kuwaharaShader);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            blitTargetDescriptor.depthBufferBits = 0;
            //cmd.GetTemporaryRT(destId, blitTargetDescriptor, FilterMode.Bilinear);
            var renderer = renderingData.cameraData.renderer;
            src = renderer.cameraColorTargetHandle;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!renderingData.cameraData.postProcessEnabled)
                return;

            if (kuwaharaMaterial is null)
            {
                Debug.LogError("Kuwahara material not created.");
                return;
            }

            cmd = CommandBufferPool.Get(cmdName);
            var camera = renderingData.cameraData.camera;

            float radius = Mathf.Ceil(settings.radius);
            kuwaharaMaterial.SetVector("_Radius", new Vector4(radius, radius, 0.0f, 0.0f));

            var width = camera.scaledPixelWidth;
            var height = camera.scaledPixelHeight;
            cmd.GetTemporaryRT(destId, width, height, 0, FilterMode.Trilinear, RenderTextureFormat.Default);

            cmd.Blit(src, destId, kuwaharaMaterial, 0);
            cmd.Blit(destId, src);

            cmd.ReleaseTemporaryRT(destId);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}