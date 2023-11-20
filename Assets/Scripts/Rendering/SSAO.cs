using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
public class SSAOSettings
{
}

public class SSAO : ScriptableRendererFeature
{
    public SSAOSettings settings;

    private Shader shader;
    private Material material;

    SSAOPass ssaoPass;

    public override void Create()
    {
        ssaoPass = new SSAOPass(RenderPassEvent.AfterRenderingOpaques);
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

    private RTHandle sourceTexture;
    private RTHandle destinationTexture;
    private RTHandle ssaoTexture;
    private const string ssaoTextureName = "_SSAOTexture";
    private RenderTextureDescriptor ssaoDescriptor;

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
        ssaoDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        ssaoDescriptor.msaaSamples = 1;
        ssaoDescriptor.depthBufferBits = 0;

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

        destinationTexture = renderingData.cameraData.renderer.cameraColorTargetHandle;
        
        using (new ProfilingScope(cmd, profilingSampler)) {
            //CoreUtils.SetRenderTarget(cmd, ssaoTexture);
            //cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
            Blitter.BlitCameraTexture(cmd, ssaoTexture, destinationTexture, material, 0);
            //Blitter.BlitCameraTexture(cmd, ssaoTexture, destinationTexture, material, 0);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd) {
        sourceTexture = null;
        destinationTexture = null;
    }

    public void Dispose(){
        ssaoTexture?.Release();
    }
}