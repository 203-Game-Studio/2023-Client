using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
public class SSAOSettings
{
    [SerializeField] internal float Intensity = 0.5f;
    [SerializeField] internal float Radius = 0.25f;
}

public class SSAO : ScriptableRendererFeature
{
    public SSAOSettings settings;

    private Shader shader;
    private Material material;

    SSAOPass ssaoPass;

    public override void Create()
    {
        ssaoPass = new SSAOPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(GetMaterials()){
            ssaoPass.Setup(settings, material, renderer);
            renderer.EnqueuePass(ssaoPass);
        }
    }

    protected override void Dispose(bool disposing) {
       CoreUtils.Destroy(material);
       ssaoPass?.Dispose();
       ssaoPass = null;
    }

    private bool GetMaterials() {
       if (shader == null)
           shader = Shader.Find("John/SSAO");
       if (material == null && shader != null)
           material = CoreUtils.CreateEngineMaterial(shader);
       return material != null;
    }
    
}

public class SSAOPass : ScriptableRenderPass
{
    private SSAOSettings settings;
    private Material material;
    private ScriptableRenderer renderer;
    private ProfilingSampler profilingSampler = new ProfilingSampler("SSAO");
    private RTHandle ssaoTexture0;
    private RTHandle ssaoTexture1;
    private RTHandle ssaoTexture;
    private const string ssaoTextureName0 = "_SSAOTexture0";
    private const string ssaoTextureName1 = "_SSAOTexture1";
    private const string ssaoTextureName = "_ScreenSpaceOcclusionTexture";
    private RenderTextureDescriptor ssaoDescriptor;

    private static readonly int projectionParams2ID = Shader.PropertyToID("_ProjectionParams2"),
                ssaoTextureID = Shader.PropertyToID("_ScreenSpaceOcclusionTexture"),
                ssaoParamsID = Shader.PropertyToID("_SSAOParams"),
                cameraViewXExtentID = Shader.PropertyToID("_CameraViewXExtent"),
                cameraViewYExtentID = Shader.PropertyToID("_CameraViewYExtent"),
                cameraViewTopLeftCornerID = Shader.PropertyToID("_CameraViewTopLeftCorner"),
                sourceSizeID = Shader.PropertyToID("_SourceSize");

    public void Setup(SSAOSettings settings, Material material, ScriptableRenderer renderer)
    {
        this.settings = settings;
        this.material = material;
        this.renderer = renderer;

        if (material == null) {
            Debug.LogErrorFormat("{0} Missing material.", GetType().Name);
            return;
        }

        ConfigureInput(ScriptableRenderPassInput.Normal);
    }

    public SSAOPass(RenderPassEvent evt)
    {
        renderPassEvent = evt;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
        Matrix4x4 v = renderingData.cameraData.GetViewMatrix();
        Matrix4x4 p = renderingData.cameraData.GetProjectionMatrix();
        Matrix4x4 vp = p * v;

        v.SetColumn(3, new Vector4(0.0f,0.0f,0.0f,1.0f));
        Matrix4x4 cviewProj = p * v;
        Matrix4x4 cviewProjInv = cviewProj.inverse;

        var near = renderingData.cameraData.camera.nearClipPlane;
        Vector4 topLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1.0f, 1.0f, -1.0f, 1.0f));
        Vector4 topRightCorner = cviewProjInv.MultiplyPoint(new Vector4(1.0f, 1.0f, -1.0f, 1.0f));
        Vector4 bottomLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1.0f, -1.0f, -1.0f, 1.0f));

        Vector4 cameraXExtent = topRightCorner - topLeftCorner;
        Vector4 cameraYExtent = bottomLeftCorner - topLeftCorner;

        near = renderingData.cameraData.camera.nearClipPlane;

        material.SetVector(cameraViewTopLeftCornerID, topLeftCorner);
        material.SetVector(cameraViewXExtentID, cameraXExtent);
        material.SetVector(cameraViewYExtentID, cameraYExtent);
        material.SetVector(projectionParams2ID, new Vector4(1.0f / near, renderingData.cameraData.worldSpaceCameraPos.x, renderingData.cameraData.worldSpaceCameraPos.y, renderingData.cameraData.worldSpaceCameraPos.z));

        material.SetVector(ssaoParamsID, new Vector4(settings.Intensity, settings.Radius));

        ssaoDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        ssaoDescriptor.msaaSamples = 1;
        ssaoDescriptor.depthBufferBits = 0;

        RenderingUtils.ReAllocateIfNeeded(ref ssaoTexture0, ssaoDescriptor, name: ssaoTextureName0);
        RenderingUtils.ReAllocateIfNeeded(ref ssaoTexture1, ssaoDescriptor, name: ssaoTextureName1);
        RenderingUtils.ReAllocateIfNeeded(ref ssaoTexture, ssaoDescriptor, name: ssaoTextureName);

        ConfigureTarget(renderer.cameraColorTargetHandle);
        ConfigureClear(ClearFlag.None, Color.white);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!renderingData.cameraData.postProcessEnabled)
            return;

        var cmd = CommandBufferPool.Get();
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        
        using (new ProfilingScope(cmd, profilingSampler)) {
            cmd.SetGlobalVector(sourceSizeID, new Vector4(ssaoDescriptor.width, ssaoDescriptor.height, 1.0f / ssaoDescriptor.width, 1.0f / ssaoDescriptor.height));
            cmd.SetGlobalTexture(ssaoTextureID, ssaoTexture);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, true);

            // SSAO
            RTHandle cameraDepthTargetHandle = renderer.cameraDepthTargetHandle;
            Blitter.BlitCameraTexture(cmd, cameraDepthTargetHandle, ssaoTexture0, material, 0);

            // Horizontal Blur
            Blitter.BlitCameraTexture(cmd, ssaoTexture0, ssaoTexture1, material, 1);

            // Vertical Blur
            Blitter.BlitCameraTexture(cmd, ssaoTexture1, ssaoTexture0, material, 2);

            // Final Pass
            Blitter.BlitCameraTexture(cmd, ssaoTexture0, ssaoTexture, material, 3);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd) {
        //CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, false);
    }

    public void Dispose(){
        ssaoTexture0?.Release();
        ssaoTexture1?.Release();
    }
}