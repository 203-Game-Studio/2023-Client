using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenSpaceReflectionFeature: ScriptableRendererFeature
{
    [System.Serializable]
    public class ScreenSpaceReflectionSettings
    {
        public RenderPassEvent Event = RenderPassEvent.AfterRenderingTransparents;

        public float MaxSteps = 32;
        public float MaxDistance = 10;
        [Range(0, 1)]public float Thickness = 0.1f;
        [Range(0, 1)]public float ReflectionStride = 0.5f;
        [Range(0, 1)]public float ReflectionJitter = 1.0f;
        [Range(-0.5f, 0.5f)]public float ReflectionBlurSpread = 0;
        [Range(0, 1)]public float LuminanceCloseOpThreshold = 0.5f;
    }

    public ScreenSpaceReflectionSettings settings = new ScreenSpaceReflectionSettings();
    private ScreenSpaceReflectionPass screenSpaceReflectionPass;
    private SSRPrePass drawObjectsPass;

    RenderTargetHandle ssrBufHandle;
    public override void Create()
    {
        drawObjectsPass = new SSRPrePass(RenderQueueRange.opaque, -1);
        drawObjectsPass.renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
        ssrBufHandle.Init("_SSRBuffer");
        
        screenSpaceReflectionPass ??= new();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData renderingData)
    {
        drawObjectsPass.Setup(renderingData.cameraData.cameraTargetDescriptor, ssrBufHandle);
        renderer.EnqueuePass(drawObjectsPass);
        screenSpaceReflectionPass.Setup(settings.Event, settings, (UniversalRenderer)renderer);
        renderer.EnqueuePass(screenSpaceReflectionPass);
    }

    class SSRPrePass : ScriptableRenderPass
    {
        private RenderTargetHandle depthAttachmentHandle { get; set; }

        internal RenderTextureDescriptor descriptor { get; set; }

        private FilteringSettings m_FilteringSettings;

        string m_ProfilerTag = "SSR Prepass";
        ShaderTagId m_ShaderTagId = new ShaderTagId("SSR");


        public SSRPrePass(RenderQueueRange renderQueueRange,LayerMask layerMask)
        {
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);

        }
        public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle depthAttachmentHandle)
        {
            this.depthAttachmentHandle = depthAttachmentHandle;

            baseDescriptor.colorFormat = RenderTextureFormat.ARGB32;
            descriptor = baseDescriptor; 
        }
        
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(depthAttachmentHandle.id, descriptor, FilterMode.Point);

            ConfigureTarget(depthAttachmentHandle.Identifier());
            ConfigureClear(ClearFlag.All, Color.black);
        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

           using(new ProfilingScope(cmd,new ProfilingSampler(m_ProfilerTag)))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;

                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings);

                cmd.SetGlobalTexture("_SSRBuffer", depthAttachmentHandle.id);
   
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);


        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (depthAttachmentHandle != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(depthAttachmentHandle.id);
                depthAttachmentHandle = RenderTargetHandle.CameraTarget;
            }
        }

    }

    public class ScreenSpaceReflectionPass : ScriptableRenderPass
    {
        private enum ScreenSpacePass
        {
            Reflection,
            VerticalBlur,
            HorizontalBlur,
            Composite
        }

        private const string CommandBufferTag = "ScreenSpaceReflectionPass";

        private Material material;

        private RTHandle renderTarget;

        private RTHandle oddHandle;
        private RTHandle evenHandle;

        private UniversalRenderer renderer;

        private const string shaderName = "ScreenSpaceReflectionShader";
        private static readonly int oddBufferID = Shader.PropertyToID("_OddBuffer");
        private static readonly int evenBufferID = Shader.PropertyToID("_EvenBuffer");

        public static readonly int s_ProjectionParams2ID = Shader.PropertyToID("_ProjectionParams2");
        public static readonly int s_CameraViewTopLeftCornerID = Shader.PropertyToID("_CameraViewTopLeftCorner");
        public static readonly int s_CameraViewXExtentID = Shader.PropertyToID("_CameraViewXExtent");
        public static readonly int s_CameraViewYExtentID = Shader.PropertyToID("_CameraViewYExtent");

        public void Setup(RenderPassEvent renderPassEvent, 
            ScreenSpaceReflectionFeature.ScreenSpaceReflectionSettings settings,
            UniversalRenderer renderer)
        {
            this.renderPassEvent = renderPassEvent;
            this.renderer = renderer;

            material = CoreUtils.CreateEngineMaterial(shaderName);
            ConfigureInput(ScriptableRenderPassInput.Color);
            ConfigureInput(ScriptableRenderPassInput.Depth);

            material.SetFloat("_MaxSteps", settings.MaxSteps);
            material.SetFloat("_MaxDistance", settings.MaxDistance);
            material.SetFloat("_Thickness", settings.Thickness);
            material.SetFloat("_ReflectionStride", settings.ReflectionStride);
            material.SetFloat("_ReflectionJitter", settings.ReflectionJitter);
            material.SetFloat("_BlurSize", 1.0f + settings.ReflectionBlurSpread);
            material.SetFloat("_LuminanceCloseOpThreshold", settings.LuminanceCloseOpThreshold);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            renderTarget = renderer.cameraColorTargetHandle;
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref oddHandle, descriptor, FilterMode.Point, name: "oddHandle");
            RenderingUtils.ReAllocateIfNeeded(ref evenHandle, descriptor, FilterMode.Point, name: "evenHandle");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get(CommandBufferTag);

            int eyeIndex = 0;
            Matrix4x4 view = renderingData.cameraData.GetViewMatrix(eyeIndex);
            Matrix4x4 proj = renderingData.cameraData.GetProjectionMatrix(eyeIndex);
            Matrix4x4 cview = view;
            cview.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
            Matrix4x4 cviewProj = proj * cview;
            Matrix4x4 cviewProjInv = cviewProj.inverse;

            Vector4 topLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, 1, -1, 1));
            Vector4 topRightCorner = cviewProjInv.MultiplyPoint(new Vector4(1, 1, -1, 1));
            Vector4 bottomLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, -1, -1, 1));
            Vector4 farCentre = cviewProjInv.MultiplyPoint(new Vector4(0, 0, 1, 1));
            material.SetVector(s_ProjectionParams2ID, new Vector4(1.0f / renderingData.cameraData.camera.nearClipPlane, 0.0f, 0.0f, 0.0f));
            material.SetVector(s_CameraViewTopLeftCornerID, topLeftCorner);
            material.SetVector(s_CameraViewXExtentID, topRightCorner - topLeftCorner);
            material.SetVector(s_CameraViewYExtentID, bottomLeftCorner - topLeftCorner);

            cmd.SetRenderTarget(oddHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            //cmd.ClearRenderTarget(false,true,Color.black);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, (int)ScreenSpacePass.Reflection);

            cmd.SetGlobalTexture("_MainTex", oddHandle);
            cmd.SetRenderTarget(evenHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, (int)ScreenSpacePass.VerticalBlur);

            cmd.SetGlobalTexture("_MainTex", evenHandle);
            cmd.SetRenderTarget(oddHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            //cmd.Blit(evenHandle, oddHandle, material, (int)ScreenSpacePass.HorizontalBlur);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, (int)ScreenSpacePass.HorizontalBlur);

            cmd.SetGlobalTexture("_MainTex", oddHandle);
            cmd.SetRenderTarget(evenHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, (int)ScreenSpacePass.Composite);

            cmd.SetGlobalTexture("_SSRTexture", evenHandle);
            cmd.Blit(evenHandle, renderTarget);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            //evenHandle.Release();
            //oddHandle.Release();
        }
    }
}