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
        if (!Application.isPlaying || renderingData.cameraData.cameraType == CameraType.Preview) {
            return;
        }

        depthPass.Setup();
        renderer.EnqueuePass(depthPass);

        drawpass.Setup();
        renderer.EnqueuePass(drawpass);
    }

    public override void Create()
    {
        InitDepthTexture();
        drawpass ??= new(settings, depthTexture);
        depthPass ??= new(depthTexture);
    }

    void InitDepthTexture() {
        if(depthTexture != null) return;
        RenderTextureDescriptor depthDesc = new RenderTextureDescriptor(depthTextureSize, depthTextureSize, RenderTextureFormat.RHalf);
        depthDesc.autoGenerateMips = false;
        depthDesc.useMipMap = true;
        RenderingUtils.ReAllocateIfNeeded(ref depthTexture, depthDesc, FilterMode.Point, name: "Depth Mipmap Texture");
    }

    protected override void Dispose(bool disposing) {
    }

    public class DepthGeneratorPass : ScriptableRenderPass {
        RTHandle depthTexture;
        Material depthTextureMaterial;
        const string passName = "Depth Generate Pass";

        public DepthGeneratorPass(RTHandle depthTexture) {
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
            //Shader.Find如果没引用打包会打不进去，后面有时间改下
            depthTextureMaterial = new Material(Shader.Find("John/DepthGeneratorShader"));
            this.depthTexture = depthTexture;
        }

        public void Setup() {
            ConfigureInput(ScriptableRenderPassInput.Depth);
            ConfigureClear(ClearFlag.None, Color.white);
            ConfigureTarget(this.depthTexture);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            //if(renderingData.cameraData.cameraType != CameraType.Game) return;
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
        private ComputeBuffer clusterResult;
        private ComputeBuffer triangleResult;

        private ComputeBuffer debugColorBuffer;

        private Camera mainCamera;
        private ClusterizerUtil.MeshData meshData;
        private RTHandle depthTexture;

        private List<uint> args;
        private ComputeBuffer argsBuffer;
        private ComputeShader cullingShader;
        private int clusterKernel;
        private int triangleKernel;
        private ComputeBuffer countBuffer;
        private uint[] countBufferData = new uint[1] { 0 };

        public ComputeBuffer CreateBufferAndSetData<T>(T[] array, int stride) where T : struct
        {
            var buffer = new ComputeBuffer(array.Length, stride);
            buffer.SetData(array);
            return buffer;
        }

        public unsafe GPUDrivenRenderPass(Settings settings, RTHandle depthTexture){
            this.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            this.settings = settings;
            this.cullingShader = settings.cullingShader;
            this.depthTexture = depthTexture;
            material = CoreUtils.CreateEngineMaterial(settings.shader);
            clusterKernel = cullingShader.FindKernel("ClusterCulling");
            triangleKernel = cullingShader.FindKernel("TriangleCulling");
            mainCamera = Camera.main;

            meshData = ClusterizerUtil.LoadMeshDataFromFile("default");
            if(meshData.vertices.Length == 0) return;
            verticesBuffer = CreateBufferAndSetData(meshData.vertices, sizeof(Vector3));
            meshletBuffer = CreateBufferAndSetData(meshData.meshlets, sizeof(ClusterizerUtil.Meshlet));
            meshletVerticesBuffer = CreateBufferAndSetData(meshData.meshletVertices, sizeof(uint));
            meshletTrianglesBuffer = CreateBufferAndSetData(meshData.meshletTriangles, sizeof(uint));
            var instanceData = new Matrix4x4[]{Matrix4x4.identity};
            instanceDataBuffer = CreateBufferAndSetData(instanceData, sizeof(Matrix4x4));
            clusterResult = new ComputeBuffer(meshData.meshlets.Length, sizeof(ClusterizerUtil.Meshlet),
                ComputeBufferType.Append);
            triangleResult = new ComputeBuffer(meshData.meshlets.Length*64, sizeof(uint)*2, ComputeBufferType.Append);

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
            material.SetBuffer("_TriangleResult", triangleResult);
            material.SetBuffer("_ClusterCullingResult", clusterResult);
            
            countBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.IndirectArguments);

            cullingShader.SetInt("_MeshletCount", meshData.meshlets.Length);
            cullingShader.SetInt("depthTextureSize", depthTextureSize);

            cullingShader.SetBuffer(clusterKernel, "_MeshletBuffer", meshletBuffer);
            cullingShader.SetBuffer(clusterKernel, "_ClusterResult", clusterResult);
            cullingShader.SetBuffer(clusterKernel, "_InstanceDataBuffer", instanceDataBuffer);
            cullingShader.SetTexture(clusterKernel, "_HizTexture", depthTexture);

            cullingShader.SetBuffer(triangleKernel, "_ClusterCullingResult", clusterResult);
            cullingShader.SetBuffer(triangleKernel, "_TriangleResult", triangleResult);
            cullingShader.SetBuffer(triangleKernel, "_InstanceDataBuffer", instanceDataBuffer);
            cullingShader.SetTexture(triangleKernel, "_HizTexture", depthTexture);
            cullingShader.SetBuffer(triangleKernel, "_VerticesBuffer", verticesBuffer);
            cullingShader.SetBuffer(triangleKernel, "_MeshletVerticesBuffer", meshletVerticesBuffer);
            cullingShader.SetBuffer(triangleKernel, "_MeshletTrianglesBuffer", meshletTrianglesBuffer);

            args = new List<uint>(){3, (uint)meshData.meshlets.Length, 0, 0, 0};
            argsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);
        }

        public void Setup(){
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData){
            var cmd = CommandBufferPool.Get(passName);
            using (new ProfilingScope(cmd, profilingSampler)){
                cmd.Clear();

                Matrix4x4 vpMatrix = GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, false) * mainCamera.worldToCameraMatrix;
                cullingShader.SetMatrix("_VPMatrix", vpMatrix);
                cullingShader.SetVector("_CameraPos", mainCamera.transform.position);
                clusterResult.SetCounterValue(0);
                cullingShader.Dispatch(clusterKernel, 1 + (meshData.meshlets.Length / 64), 1, 1);

                ComputeBuffer.CopyCount(clusterResult, countBuffer, 0);
                countBuffer.GetData(countBufferData);
                uint clusterCount = countBufferData[0];
                //Debug.LogError($"{count*64}/{meshData.meshlets.Length*64}----{(float)(count)/meshData.meshlets.Length}");
                if(clusterCount <= 0) return;
                triangleResult.SetCounterValue(0);
                //cullingShader.SetInt("_TriangleCount", (int)clusterCount * 64);
                cullingShader.Dispatch(triangleKernel, 1, (int)clusterCount, 1);
                ComputeBuffer.CopyCount(triangleResult, countBuffer, 0);
                countBuffer.GetData(countBufferData);
                uint triangleCount = countBufferData[0];
                if(triangleCount <= 0) return;
                args[1] = triangleCount;
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
            clusterResult?.Dispose();
            debugColorBuffer?.Dispose();
            argsBuffer?.Dispose();
            countBuffer?.Dispose();
            depthTexture?.Release();
            depthTexture = null;
        }

        ~GPUDrivenRenderPass(){
            Dispose();
        }
    }
}