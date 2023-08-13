using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrassRenderFeature : ScriptableRendererFeature
{
    private GrassRenderPass pass = null;
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(renderingData.cameraData.renderType == CameraRenderType.Base){
            renderer.EnqueuePass(pass);
        }
    }
    public override void Create()
    {
        pass ??= new();
    }
    public class GrassRenderPass : ScriptableRenderPass
    {
        public GrassRenderPass(){
            this.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }

        private const string bufferName = "GrassBuffer";

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData){
          
            var cmd = CommandBufferPool.Get(bufferName);
            try{
                cmd.Clear();
                var index = 0;
                //获取所有草块 逐个调用DrawMeshInstancedProcedural
                foreach(var grassTerrian in Grass.actives){
                    if(!grassTerrian || !grassTerrian.material){
                        continue;
                    }
                    grassTerrian.UpdateMaterialProperties();
                    cmd.DrawMeshInstancedProcedural(GrassUtil.unitMesh, 0, grassTerrian.material, 0,
                        grassTerrian.grassCount, grassTerrian.materialPropertyBlock);
                    ++index;
                }
                context.ExecuteCommandBuffer(cmd);
            }
            finally{
                CommandBufferPool.Release(cmd);
            }
        }
    }
}