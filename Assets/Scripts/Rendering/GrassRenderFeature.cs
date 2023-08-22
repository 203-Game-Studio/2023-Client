using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrassRenderFeature : ScriptableRendererFeature
{
    private GrassRenderPass pass = null;

    //草材质
    public Material grassMaterial;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(renderingData.cameraData.renderType == CameraRenderType.Base){
            renderer.EnqueuePass(pass);
        }
    }
    
    public override void Create()
    {
        pass ??= new(grassMaterial);
    }

    public class GrassRenderPass : ScriptableRenderPass
    {
        private Material grassMaterial;

        public GrassRenderPass(Material grassMaterial){
            this.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            this.grassMaterial = grassMaterial;
        }

        private const string bufferName = "GrassBuffer";

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData){
            if(!Application.isPlaying) return;
            int count = Grass.Instance.cullResultCount;
            //可能出现0数量，比如朝天看
            if(count <= 0) return;
            var cmd = CommandBufferPool.Get(bufferName);
            try{
                cmd.Clear();
                grassMaterial.SetBuffer("_LocalToWorldMats", Grass.Instance.CullResultBuffer);
                cmd.DrawMeshInstancedProcedural(GrassUtil.unitMesh, 0, grassMaterial, 0, count);
                context.ExecuteCommandBuffer(cmd);
            }
            finally{
                CommandBufferPool.Release(cmd);
            }
        }
    }
}