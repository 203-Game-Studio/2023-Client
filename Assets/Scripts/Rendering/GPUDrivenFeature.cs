using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GPUDrivenFeature : ScriptableRendererFeature
{
    private GPUDrivenRenderPass drawpass = null;
    private DepthGeneratorPass depthPass = null;

    [System.Serializable]
    public class Settings
    {
        public Shader shader;
        public ComputeShader cullingShader;
    }
    public Settings settings = new Settings();

    private RTHandle depthTexture;
    static int _depthTextureSize = 0;
    public static int depthTextureSize {
        get {
            if(_depthTextureSize == 0)
                _depthTextureSize = Mathf.NextPowerOfTwo(Mathf.Max(Screen.width, Screen.height));
            return _depthTextureSize;
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Preview) {
            return;
        }
        InitDepthTexture();

        depthPass.Setup(depthTexture);
        renderer.EnqueuePass(depthPass);

        drawpass.Setup(depthTexture);
        renderer.EnqueuePass(drawpass);
    }

    public override void Create()
    {
        drawpass ??= new(settings);
        depthPass ??= new();
    }

    void InitDepthTexture() {
        if(depthTexture != null) return;
        RenderTextureDescriptor depthDesc = new RenderTextureDescriptor(depthTextureSize, depthTextureSize, RenderTextureFormat.RHalf);
        depthDesc.autoGenerateMips = false;
        depthDesc.useMipMap = true;
        RenderingUtils.ReAllocateIfNeeded(ref depthTexture, depthDesc, FilterMode.Point, name: "Depth Mipmap Texture");
    }

    protected override void Dispose(bool disposing) {
        depthTexture?.Release();
        depthTexture = null;
    }

    public class DepthGeneratorPass : ScriptableRenderPass {
        RTHandle depthTexture;
        Material depthTextureMaterial;
        int depthTextureShaderID;
        const string passName = "Depth Generate Pass";

        public DepthGeneratorPass() {
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
            depthTextureShaderID = Shader.PropertyToID("_CameraDepthTexture");
            //Shader.Find如果没引用打包会打不进去，后面有时间改下
            depthTextureMaterial = new Material(Shader.Find("John/DepthGeneratorShader"));
        }

        public void Setup(RTHandle depthTexture) {
            ConfigureInput(ScriptableRenderPassInput.Depth);
            ConfigureClear(ClearFlag.None, Color.white);
            this.depthTexture = depthTexture;
            ConfigureTarget(this.depthTexture);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            if(!Application.isPlaying || renderingData.cameraData.cameraType != CameraType.Game) return;
            var cmd = CommandBufferPool.Get(passName);
            using (new ProfilingScope(cmd, profilingSampler)){
                ///////////////////////////////
                ///生成深度图的mipmap
                ///////////////////////////////
                int width = depthTexture.rt.width;
                int mipmapLevel = 0;
                RenderTexture currentRenderTexture = null;
                RenderTexture preRenderTexture = null;
                while(width > 8) {
                    currentRenderTexture = RenderTexture.GetTemporary(width, width, 0, RenderTextureFormat.RHalf);
                    currentRenderTexture.filterMode = FilterMode.Point;
                    if(preRenderTexture == null) {
                        cmd.Blit(preRenderTexture, currentRenderTexture, depthTextureMaterial, 1);
                    }
                    else {
                        cmd.Blit(preRenderTexture, currentRenderTexture, depthTextureMaterial, 0);
                        RenderTexture.ReleaseTemporary(preRenderTexture);
                    }
                    cmd.CopyTexture(currentRenderTexture, 0, 0, depthTexture, 0, mipmapLevel);
                    preRenderTexture = currentRenderTexture;

                    width /= 2;
                    mipmapLevel++;
                }
                RenderTexture.ReleaseTemporary(preRenderTexture);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }
        }
    }

    public class GPUDrivenRenderPass : ScriptableRenderPass
    {
        private Settings settings;

        private const string passName = "GPU Driven Pass";

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
        private RTHandle depthTexture;

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
            cullingShader.SetInt("depthTextureSize", depthTextureSize);
            cullingShader.SetTexture(cullingKernel, "_HizTexture", depthTexture);

            args = new List<uint>(){64*3, (uint)meshData.meshlets.Length, 0, 0, 0};
            argsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);
        }

        public void Setup(RTHandle depthTexture){
            this.depthTexture = depthTexture;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData){
            if(!Application.isPlaying) return;
            var cmd = CommandBufferPool.Get(passName);
            using (new ProfilingScope(cmd, profilingSampler)){
                cmd.Clear();

                Matrix4x4 vpMatrix = GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, false) * mainCamera.worldToCameraMatrix;
                cullingShader.SetMatrix("_VPMatrix", vpMatrix);
                cullResult.SetCounterValue(0);
                cullingShader.Dispatch(cullingKernel, 1 + (meshData.meshlets.Length / 32), 1, 1);

                ComputeBuffer.CopyCount(cullResult, countBuffer, 0);
                countBuffer.GetData(countBufferData);
                uint count = countBufferData[0];
                if(count <= 0) return;
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