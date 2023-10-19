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

        private const string bufferName = "GPU Driven Pass";

        private Material material;
        private ComputeBuffer argsBuffer;
        private ComputeBuffer verticesBuffer;
        private ComputeBuffer meshletBuffer;
        private ComputeBuffer meshletBoundsBuffer;
        private ComputeBuffer instanceDataBuffer;
        private ComputeBuffer meshletVerticesBuffer;
        private ComputeBuffer meshletTrianglesBuffer;

        private ComputeBuffer debugColorBuffer;

        public GPUDrivenRenderPass(Settings settings){
            this.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            this.settings = settings;
            material = CoreUtils.CreateEngineMaterial(Shader.Find("John/RenderObjectLit"));

            var data = ClusterizerUtil.LoadMeshDataFromFile("default");
            if(data.vertices.Length == 0) return;
            verticesBuffer = new ComputeBuffer(data.vertices.Length, 4*3);
            verticesBuffer.SetData(data.vertices);
            meshletBuffer = new ComputeBuffer(data.meshlets.Length, 4*4);
            meshletBuffer.SetData(data.meshlets);
            meshletBoundsBuffer = new ComputeBuffer(data.meshletBounds.Length, 4*13);
            meshletBoundsBuffer.SetData(data.meshletBounds);
            meshletVerticesBuffer = new ComputeBuffer(data.meshletVertices.Length, 4);
            meshletVerticesBuffer.SetData(data.meshletVertices);
            meshletTrianglesBuffer = new ComputeBuffer(data.meshletTriangles.Length, 4);
            uint[] meshletTriangles = new uint[data.meshletTriangles.Length];
            for(int i = 0; i < meshletTriangles.Length;++i){
                meshletTriangles[i] = data.meshletTriangles[i];
            }
            meshletTrianglesBuffer.SetData(meshletTriangles);

            instanceDataBuffer = new ComputeBuffer(1, 16*4);
            instanceDataBuffer.SetData(new Matrix4x4[]{Matrix4x4.identity});

            //debug
            debugColorBuffer = new ComputeBuffer(100, 3*4);
            Vector3[] color = new Vector3[100];
            for(int i = 0; i < 100; ++i){
                color[i] = new Vector3(UnityEngine.Random.Range(0, 1.0f), 
                    UnityEngine.Random.Range(0, 1.0f),UnityEngine.Random.Range(0, 1.0f));
            }
            debugColorBuffer.SetData(color);
            material.SetBuffer("_DebugColorBuffer", debugColorBuffer);
            material.SetInt("_ColorCount", 100);

            material.SetBuffer("_VerticesBuffer", verticesBuffer);
            material.SetBuffer("_MeshletBuffer", meshletBuffer);
            material.SetBuffer("_MeshletBoundsBuffer", meshletBoundsBuffer);
            material.SetBuffer("_MeshletVerticesBuffer", meshletVerticesBuffer);
            material.SetBuffer("_MeshletTrianglesBuffer", meshletTrianglesBuffer);
            material.SetBuffer("_InstanceDataBuffer", instanceDataBuffer);

            List<uint> args = new List<uint>(){64*3, (uint)data.meshlets.Length, 0, 0, 0};
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
                /*Matrix4x4 view = renderingData.cameraData.GetViewMatrix(0);
                Matrix4x4 proj = renderingData.cameraData.GetProjectionMatrix(0);
                Matrix4x4 VP = proj * view;
                material.SetMatrix("_VPMatrix", VP);*/
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