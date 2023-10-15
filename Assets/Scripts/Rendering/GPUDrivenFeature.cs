using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GPUDrivenFeature : ScriptableRendererFeature
{
    private GPUDrivenRenderPass pass = null;

    [System.Serializable]
    public class Settings
    {
        public Shader shader;
    }
    public Settings settings = new Settings();

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Preview) {
            return;
        }

        pass.Setup();
        renderer.EnqueuePass(pass);
    }

    public override void Create()
    {
        pass ??= new(settings);
    }

    public class GPUDrivenRenderPass : ScriptableRenderPass
    {
        private Settings settings;

        private const string bufferName = "Terrain Pass";

        private Material material;
        private ComputeBuffer argsBuffer;
        private ComputeBuffer _VerticesBuffer;
        private ComputeBuffer _MeshletBuffer;
        private ComputeBuffer _MeshletVerticesBuffer;
        private ComputeBuffer _MeshletTrianglesBuffer;

        public GPUDrivenRenderPass(Settings settings){
            this.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            this.settings = settings;
            material = CoreUtils.CreateEngineMaterial(Shader.Find("John/RenderObjectLit"));

            var data = ClusterizerUtil.LoadMeshDataFromFile("default");
            if(data.vertices.Length == 0) return;
            _VerticesBuffer = new ComputeBuffer(data.vertices.Length, 4*3);
            _VerticesBuffer.SetData(data.vertices);
            _MeshletBuffer = new ComputeBuffer(data.meshlets.Length, 4*4);
            _MeshletBuffer.SetData(data.meshlets);
            _MeshletVerticesBuffer = new ComputeBuffer(data.meshletVertices.Length, 4);
            _MeshletVerticesBuffer.SetData(data.meshletVertices);
            _MeshletTrianglesBuffer = new ComputeBuffer(data.meshletTriangles.Length, 4);
            uint[] meshletTriangles = new uint[data.meshletTriangles.Length];
            for(int i = 0; i < meshletTriangles.Length;++i){
                meshletTriangles[i] = data.meshletTriangles[i];
            }
            _MeshletTrianglesBuffer.SetData(meshletTriangles);

            material.SetBuffer("_VerticesBuffer", _VerticesBuffer);
            material.SetBuffer("_MeshletBuffer", _MeshletBuffer);
            material.SetBuffer("_MeshletVerticesBuffer", _MeshletVerticesBuffer);
            material.SetBuffer("_MeshletTrianglesBuffer", _MeshletTrianglesBuffer);

            List<uint> args = new List<uint>(){64, (uint)data.meshlets.Length, 0, 0, 0};
            argsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);
        }

        public void Setup(){
            
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData){
            if(!Application.isPlaying) return;
            var cmd = CommandBufferPool.Get(bufferName);
            using (new ProfilingScope(cmd, profilingSampler)){
                cmd.Clear();
                cmd.DrawProceduralIndirect(Matrix4x4.identity, material, 0, MeshTopology.Triangles,
                    argsBuffer, 0);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }
        }

        public void Dispose(){
        }

        ~GPUDrivenRenderPass(){
            Dispose();
        }
    }
}