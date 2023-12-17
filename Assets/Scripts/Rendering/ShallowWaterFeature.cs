using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ShallowWaterFeature : ScriptableRendererFeature
{
    private ShallowWaterRenderPass waterPass = null;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(waterPass);
    }

    public override void Create()
    {
        waterPass ??= new();
    }

    public class ShallowWaterRenderPass : ScriptableRenderPass
    {
        public ShallowWaterRenderPass(){}

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData){
            var cmds = renderingData.cameraData.camera.GetCommandBuffers(CameraEvent.BeforeDepthTexture);
            if (cmds != null && cmds.Length > 0) {
                for (int i = 0; i < cmds.Length; i++) {
                    context.ExecuteCommandBuffer(cmds[i]);
                }
            }
            renderingData.cameraData.camera.RemoveCommandBuffers(CameraEvent.BeforeDepthTexture);
        }
    }
}