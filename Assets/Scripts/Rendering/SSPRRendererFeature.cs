using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SSPRRendererFeature : ScriptableRendererFeature{
    [Serializable]
    public class SSPRSettings{
        public int RTSize = 512;
        public float ReflectHeight = 0.2f;
        [Range(0.0f, 0.1f)] public float StretchIntensity = 0.1f;
        [Range(0.0f, 1.0f)] public float StretchThreshold = 0.3f;
        public float EdgeFadeOut = 0.6f;
    }
    [SerializeField] private SSPRSettings settings = new SSPRSettings();

    private SSPRPass pass;
    private ComputeShader computeShader;

    public override void Create() {
        if (pass == null) {
            if (!GetComputeShaders()) {
                Debug.LogError($"ComputeShader is null!");
                return;
            }
            pass = new SSPRPass(ref settings, ref computeShader);
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing) {
        pass?.Dispose();
        pass = null;
    }

    private bool GetComputeShaders() {
        if (computeShader == null)
            computeShader = (ComputeShader)Resources.Load("SSPR");
        return computeShader != null;
    }
    
    class SSPRPass : ScriptableRenderPass{
        private SSPRSettings settings;

        private ComputeShader computeShader;
        private int ssprKernelID, mFillHoleKernelID;
        
        private ProfilingSampler mProfilingSampler = new ProfilingSampler("SSPR");

        private RenderTextureDescriptor ssprReflectionDescriptor;
        private static readonly int reflectPlaneHeihgtID = Shader.PropertyToID("_ReflectPlaneHeight");
        private static readonly int rtSizeID = Shader.PropertyToID("_SSPRReflectionSize");
        private static readonly int ssprReflectionTextureID = Shader.PropertyToID("_SSPRReflectionTexture");
        private static readonly int cameraColorTextureID = Shader.PropertyToID("_CameraColorTexture");
        private static readonly int cameraDepthTextureID = Shader.PropertyToID("_CameraDepthTexture");
        private static readonly int ssprHeightBufferID = Shader.PropertyToID("_SSPRHeightBuffer");
        private static readonly int cameraDirectionID = Shader.PropertyToID("_CameraDirection");
        private static readonly int stretchParamsID = Shader.PropertyToID("_StretchParams");
        private static readonly int edgeFadeOutID = Shader.PropertyToID("_EdgeFadeOut");

        private RTHandle ssprReflectionTexture;
        private RTHandle ssprHeightTexture;

        private int GroupThreadX = 8;
        private int GroupThreadY = 8;
        private int GroupX;
        private int GroupY;

        public SSPRPass(ref SSPRSettings settings, ref ComputeShader computeShader) {
            this.computeShader = computeShader;
            this.settings = settings;
            this.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
            //if(renderingData.cameraData.cameraType != CameraType.Game) return;
            // 配置目标和清除
            var renderer = renderingData.cameraData.renderer;
            ConfigureTarget(renderer.cameraColorTargetHandle);
            ConfigureClear(ClearFlag.None, Color.white);

            float aspect = (float)Screen.height / Screen.width;
            // 计算线程组线程
            GroupY = Mathf.RoundToInt((float)settings.RTSize / GroupThreadY);
            GroupX = Mathf.RoundToInt(GroupY / aspect);

            ssprReflectionDescriptor = new RenderTextureDescriptor(GroupThreadX * GroupX, GroupThreadY * GroupY, RenderTextureFormat.BGRA32, 0, 0);
            ssprReflectionDescriptor.enableRandomWrite = true; // 开启
            RenderingUtils.ReAllocateIfNeeded(ref ssprReflectionTexture, ssprReflectionDescriptor, 
                FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_SSPRReflectionTexture");
            
            ssprReflectionDescriptor.colorFormat = RenderTextureFormat.RFloat;
            RenderingUtils.ReAllocateIfNeeded(ref ssprHeightTexture, ssprReflectionDescriptor, 
                FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_SSPRHeightBufferTexture");

            ssprKernelID = computeShader.FindKernel("SSPR");
            computeShader.SetFloat(reflectPlaneHeihgtID, settings.ReflectHeight);
            computeShader.SetVector(rtSizeID, new Vector4(ssprReflectionDescriptor.width, ssprReflectionDescriptor.height, 
                1.0f / (float)ssprReflectionDescriptor.width, 1.0f / (float)ssprReflectionDescriptor.height));
            computeShader.SetTexture(ssprKernelID, ssprReflectionTextureID, ssprReflectionTexture);
            computeShader.SetTexture(ssprKernelID, ssprHeightBufferID, ssprHeightTexture);
            computeShader.SetTexture(ssprKernelID, cameraColorTextureID, renderer.cameraColorTargetHandle);
            computeShader.SetTexture(ssprKernelID, cameraDepthTextureID, renderer.cameraDepthTargetHandle);
            computeShader.SetVector(cameraDirectionID, renderingData.cameraData.camera.transform.forward);
            computeShader.SetVector(stretchParamsID, new Vector4(settings.StretchIntensity, settings.StretchThreshold, 0.0f, 0.0f));
            computeShader.SetFloat(edgeFadeOutID, settings.EdgeFadeOut);

            mFillHoleKernelID = computeShader.FindKernel("FillHole");
            computeShader.SetTexture(mFillHoleKernelID, ssprReflectionTextureID, ssprReflectionTexture);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            if (computeShader == null) {
                Debug.LogError($"ComputeShader is null!");
                return;
            }

            var cmd = CommandBufferPool.Get();
            cmd.Clear();
            using (new ProfilingScope(cmd, mProfilingSampler)) {
                cmd.DispatchCompute(computeShader, ssprKernelID, GroupX, GroupY, 1);
                cmd.DispatchCompute(computeShader, mFillHoleKernelID, GroupX, GroupY, 1);

                cmd.SetGlobalTexture(ssprReflectionTextureID, ssprReflectionTexture);
                cmd.SetGlobalVector(rtSizeID, new Vector4(ssprReflectionDescriptor.width, ssprReflectionDescriptor.height, 1.0f / (float)ssprReflectionDescriptor.width, 1.0f / (float)ssprReflectionDescriptor.height));
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose() {
            ssprReflectionTexture?.Release();
            ssprReflectionTexture = null;
            ssprHeightTexture?.Release();
            ssprHeightTexture = null;
        }
    }
}