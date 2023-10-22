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
        public ComputeShader cullingShader;
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
        private ComputeBuffer verticesBuffer;
        private ComputeBuffer meshletBuffer;
        private ComputeBuffer instanceDataBuffer;
        private ComputeBuffer meshletVerticesBuffer;
        private ComputeBuffer meshletTrianglesBuffer;
        private ComputeBuffer cullResult;

        private ComputeBuffer debugColorBuffer;

        private Camera mainCamera;
        private ClusterizerUtil.MeshData meshData;

        private List<uint> args;
        private ComputeBuffer argsBuffer;
        private ComputeShader cullingShader;
        private int cullingKernel;
        private ComputeBuffer countBuffer;
        private uint[] countBufferData = new uint[1] { 0 };

        public ComputeBuffer CreateBufferAndSetData<T>(T[] array, int stride) where T : struct
        {
            var buffer = new ComputeBuffer(array.Length, stride);
            buffer.SetData(array);
            return buffer;
        }

        public unsafe GPUDrivenRenderPass(Settings settings){
            this.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            this.settings = settings;
            this.cullingShader = settings.cullingShader;
            material = CoreUtils.CreateEngineMaterial(settings.shader);
            cullingKernel = cullingShader.FindKernel("GPUCulling");
            mainCamera = Camera.main;

            meshData = ClusterizerUtil.LoadMeshDataFromFile("default");
            if(meshData.vertices.Length == 0) return;
            verticesBuffer = CreateBufferAndSetData(meshData.vertices, sizeof(Vector3));
            meshletBuffer = CreateBufferAndSetData(meshData.meshlets, sizeof(ClusterizerUtil.Meshlet));
            meshletVerticesBuffer = CreateBufferAndSetData(meshData.meshletVertices, sizeof(uint));
            meshletTrianglesBuffer = CreateBufferAndSetData(meshData.meshletTriangles, sizeof(uint));
            var instanceData = new Matrix4x4[]{Matrix4x4.identity};
            instanceDataBuffer = CreateBufferAndSetData(instanceData, sizeof(Matrix4x4));
            cullResult = new ComputeBuffer(meshData.meshlets.Length, sizeof(ClusterizerUtil.Meshlet),
                ComputeBufferType.Append);

            //debug
            Vector3[] color = new Vector3[100];
            for(int i = 0; i < 100; ++i){
                color[i] = new Vector3(UnityEngine.Random.Range(0, 1.0f), 
                    UnityEngine.Random.Range(0, 1.0f),UnityEngine.Random.Range(0, 1.0f));
            }
            debugColorBuffer = CreateBufferAndSetData(color, sizeof(Vector3));
            material.SetBuffer("_DebugColorBuffer", debugColorBuffer);
            material.SetInt("_ColorCount", 100);

            material.SetBuffer("_VerticesBuffer", verticesBuffer);
            material.SetBuffer("_MeshletVerticesBuffer", meshletVerticesBuffer);
            material.SetBuffer("_MeshletTrianglesBuffer", meshletTrianglesBuffer);
            material.SetBuffer("_InstanceDataBuffer", instanceDataBuffer);
            material.SetBuffer("_CullResult", cullResult);
            
            countBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.IndirectArguments);
            cullingShader.SetBuffer(cullingKernel, "_MeshletBuffer", meshletBuffer);
            cullingShader.SetInt("_MeshletCount", meshData.meshlets.Length);
            cullingShader.SetBuffer(cullingKernel, "_CullResult", cullResult);
            cullingShader.SetBuffer(cullingKernel, "_InstanceDataBuffer", instanceDataBuffer);

            args = new List<uint>(){64*3, (uint)meshData.meshlets.Length, 0, 0, 0};
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

                Matrix4x4 vpMatrix = GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, false) * mainCamera.worldToCameraMatrix;
                cullingShader.SetMatrix("_VPMatrix", vpMatrix);
                cullResult.SetCounterValue(0);
                cullingShader.Dispatch(cullingKernel, 1 + (meshData.meshlets.Length / 32), 1, 1);

                ComputeBuffer.CopyCount(cullResult, countBuffer, 0);
                countBuffer.GetData(countBufferData);
                uint count = countBufferData[0];
                args[1] = count;
                argsBuffer.SetData(args);

                cmd.Clear();
                cmd.DrawProceduralIndirect(Matrix4x4.identity, material, 0, MeshTopology.Triangles, argsBuffer, 0);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }
        }

        public void Dispose(){
            verticesBuffer?.Dispose();
            meshletBuffer?.Dispose();
            instanceDataBuffer?.Dispose();
            meshletVerticesBuffer?.Dispose();
            meshletTrianglesBuffer?.Dispose();
            cullResult?.Dispose();
            debugColorBuffer?.Dispose();
            argsBuffer?.Dispose();
            countBuffer?.Dispose();
        }

        ~GPUDrivenRenderPass(){
            Dispose();
        }
    }
}