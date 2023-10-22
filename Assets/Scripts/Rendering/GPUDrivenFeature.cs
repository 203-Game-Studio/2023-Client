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
        private ComputeBuffer instanceDataBuffer;
        private ComputeBuffer meshletVerticesBuffer;
        private ComputeBuffer meshletTrianglesBuffer;

        private ComputeBuffer debugColorBuffer;

        public ComputeBuffer CreateBufferAndSetData<T>(T[] array, int stride) where T : struct
        {
            var buffer = new ComputeBuffer(array.Length, stride);
            buffer.SetData(array);
            return buffer;
        }

        public unsafe GPUDrivenRenderPass(Settings settings){
            this.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            this.settings = settings;
            material = CoreUtils.CreateEngineMaterial(Shader.Find("John/RenderObjectLit"));

            var data = ClusterizerUtil.LoadMeshDataFromFile("default");
            if(data.vertices.Length == 0) return;
            verticesBuffer = CreateBufferAndSetData(data.vertices, sizeof(Vector3));
            meshletBuffer = CreateBufferAndSetData(data.meshlets, sizeof(ClusterizerUtil.Meshlet));
            meshletVerticesBuffer = CreateBufferAndSetData(data.meshletVertices, sizeof(uint));
            meshletTrianglesBuffer = CreateBufferAndSetData(data.meshletTriangles, sizeof(uint));
            var instanceData = new Matrix4x4[]{Matrix4x4.identity};
            instanceDataBuffer = CreateBufferAndSetData(instanceData, sizeof(Matrix4x4));

            //debug
            Vector3[] color = new Vector3[100];
            for(int i = 0; i < 100; ++i){
                color[i] = new Vector3(UnityEngine.Random.Range(0, 1.0f), 
                    UnityEngine.Random.Range(0, 1.0f),UnityEngine.Random.Range(0, 1.0f));
            }
            debugColorBuffer = CreateBufferAndSetData(color, sizeof(Vector3));
            //debugColorBuffer = new ComputeBuffer(100, 3*4);
            //debugColorBuffer.SetData(color);
            material.SetBuffer("_DebugColorBuffer", debugColorBuffer);
            material.SetInt("_ColorCount", 100);

            material.SetBuffer("_VerticesBuffer", verticesBuffer);
            material.SetBuffer("_MeshletBuffer", meshletBuffer);
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