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
        ShadowUtils.CustomRenderShadowSlice -= GPUDrivenRenderPass.RenderShadowmap;
        ShadowUtils.CustomRenderShadowSlice += GPUDrivenRenderPass.RenderShadowmap;
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

        private static Material material;
        private ComputeBuffer verticesBuffer;
        private ComputeBuffer meshletBuffer;
        private ComputeBuffer instanceDataBuffer;
        private ComputeBuffer meshletVerticesBuffer;
        private ComputeBuffer meshletTrianglesBuffer;
        private ComputeBuffer clusterResult;
        private ComputeBuffer shadowResult0;
        private ComputeBuffer shadowResult1;
        private ComputeBuffer shadowResult2;
        private ComputeBuffer triangleResult;

        private ComputeBuffer debugColorBuffer;

        private Camera mainCamera;
        private ClusterizerUtil.MeshData meshData;
        private RTHandle depthTexture;

        private List<uint> args;
        private ComputeBuffer argsBuffer;
        private List<uint> shadowArgs;
        private static ComputeBuffer shadowArgsBuffer0;
        private static ComputeBuffer shadowArgsBuffer1;
        private static ComputeBuffer shadowArgsBuffer2;
        private ComputeShader cullingShader;
        private int clusterKernel;
        private int triangleKernel;
        private int shadowKernel;
        private ComputeBuffer countBuffer;
        private uint[] countBufferData = new uint[1] { 0 };

        private float[] cascadeDistances;

        public ComputeBuffer CreateBufferAndSetData<T>(T[] array, int stride) where T : struct
        {
            var buffer = new ComputeBuffer(array.Length, stride);
            buffer.SetData(array);
            return buffer;
        }

        public GPUDrivenRenderPass(Settings settings, RTHandle depthTexture){
            this.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            this.settings = settings;
            this.cullingShader = settings.cullingShader;
            this.depthTexture = depthTexture;
            material = CoreUtils.CreateEngineMaterial(settings.shader);
            clusterKernel = cullingShader.FindKernel("ClusterCulling");
            triangleKernel = cullingShader.FindKernel("TriangleCulling");
            shadowKernel = cullingShader.FindKernel("ShadowCulling");
            mainCamera = Camera.main;
            cascadeDistances = GetShadowCascadesDistances();
            UpdateBuffer();
        }

        public void Setup(){
        }

        private unsafe void UpdateBuffer(){
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
            shadowResult0 = new ComputeBuffer(meshData.meshlets.Length, sizeof(ClusterizerUtil.Meshlet),
                ComputeBufferType.Append);
            shadowResult1 = new ComputeBuffer(meshData.meshlets.Length, sizeof(ClusterizerUtil.Meshlet),
                ComputeBufferType.Append);
            shadowResult2 = new ComputeBuffer(meshData.meshlets.Length, sizeof(ClusterizerUtil.Meshlet),
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
            material.SetBuffer("_ShadowCullingResult0", shadowResult0);
            material.SetBuffer("_ShadowCullingResult1", shadowResult1);
            material.SetBuffer("_ShadowCullingResult2", shadowResult2);
            
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

            cullingShader.SetBuffer(shadowKernel, "_MeshletBuffer", meshletBuffer);
            cullingShader.SetBuffer(shadowKernel, "_InstanceDataBuffer", instanceDataBuffer);
            cullingShader.SetBuffer(shadowKernel, "_ShadowResult0", shadowResult0);
            cullingShader.SetBuffer(shadowKernel, "_ShadowResult1", shadowResult1);
            cullingShader.SetBuffer(shadowKernel, "_ShadowResult2", shadowResult2);

            args = new List<uint>(){3, (uint)meshData.meshlets.Length, 0, 0, 0};
            argsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);

            shadowArgs = new List<uint>(){64*3, (uint)meshData.meshlets.Length, 0, 0, 0};
            shadowArgsBuffer0 = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
            shadowArgsBuffer0.SetData(shadowArgs);
            shadowArgsBuffer1 = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
            shadowArgsBuffer1.SetData(shadowArgs);
            shadowArgsBuffer2 = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
            shadowArgsBuffer2.SetData(shadowArgs);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData){
            var cmd = CommandBufferPool.Get(passName);
            using (new ProfilingScope(cmd, profilingSampler)){
                cmd.Clear();

                var instanceData = GPUDrivenManager.Instance.BuildInstanceData();
                /*if(GPUDrivenManager.renderObjectDicIsDirty){
                    GPUDrivenManager.renderObjectDicIsDirty = false;
                    instanceDataBuffer = CreateBufferAndSetData(instanceData, 4*4*4);
                }else*/{
                    instanceDataBuffer.SetData(instanceData);
                }

                int shadowLightIndex = renderingData.lightData.mainLightIndex;
                if (shadowLightIndex != -1) {
                    VisibleLight shadowLight = renderingData.lightData.visibleLights[shadowLightIndex];
                    Light light = shadowLight.light;
                    if (light.shadows != LightShadows.None) {
                        Vector3[] standFrustumV = GetFrustum8Point(mainCamera);
                        Vector4[] standFrustumVertices = new Vector4[8];
                        for (int i = 0; i < 8; i++)
                        {
                            standFrustumVertices[i] = new Vector4(standFrustumV[i].x, standFrustumV[i].y, standFrustumV[i].z, 0);
                        }

                        for (int i = 0; i < cascadeDistances.Length; i++)
                        {
                            standFrustumVertices[i].w = cascadeDistances[i];
                        }

                        var mainLightDir = light.transform.forward;
                        standFrustumVertices[3].w = mainCamera.farClipPlane;
                        standFrustumVertices[4].w = cascadeDistances.Length;
                        standFrustumVertices[5].w = mainLightDir.x;
                        standFrustumVertices[6].w = mainLightDir.y;
                        standFrustumVertices[7].w = mainLightDir.z;

                        cullingShader.SetVectorArray("_StandFrustumVertices", standFrustumVertices);
                        cullingShader.SetVectorArray("_CullSpheres", ShadowUtils.CullSpheres);

                        shadowResult0.SetCounterValue(0);
                        shadowResult1.SetCounterValue(0);
                        shadowResult2.SetCounterValue(0);
                        cullingShader.Dispatch(shadowKernel, 1 + (meshData.meshlets.Length / 64), 1, 1);
                        
                        //csm0
                        ComputeBuffer.CopyCount(shadowResult0, countBuffer, 0);
                        countBuffer.GetData(countBufferData);
                        shadowArgs[1] = countBufferData[0];
                        shadowArgsBuffer0.SetData(shadowArgs);
                        //csm1
                        ComputeBuffer.CopyCount(shadowResult1, countBuffer, 0);
                        countBuffer.GetData(countBufferData);
                        shadowArgs[1] = countBufferData[0];
                        shadowArgsBuffer1.SetData(shadowArgs);
                        //csm2
                        ComputeBuffer.CopyCount(shadowResult2, countBuffer, 0);
                        countBuffer.GetData(countBufferData);
                        shadowArgs[1] = countBufferData[0];
                        shadowArgsBuffer2.SetData(shadowArgs);
                    }
                }

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

        float[] GetShadowCascadesDistances()
        {
            UniversalRenderPipelineAsset urpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            int cascadeCount = urpAsset.shadowCascadeCount;
            float[] cascadeDistances = new float[cascadeCount];

            switch (cascadeCount)
            {
                case 2:
                    cascadeDistances[0] = urpAsset.cascade2Split;
                    break;
                case 3:
                    Vector2 cascade3Splits = urpAsset.cascade3Split;
                    cascadeDistances[0] = cascade3Splits.x;
                    cascadeDistances[1] = cascade3Splits.y;
                    break;
                case 4:
                    Vector3 cascade4Splits = urpAsset.cascade4Split;
                    cascadeDistances[0] = cascade4Splits.x;
                    cascadeDistances[1] = cascade4Splits.y;
                    cascadeDistances[2] = cascade4Splits.z;
                    break;
            }

            float shadowDistance = urpAsset.shadowDistance;
            for (int i = 0; i < cascadeDistances.Length - 1; i++)
            {
                cascadeDistances[i] *= shadowDistance;
            }
            cascadeDistances[cascadeDistances.Length - 1] = shadowDistance;

            return cascadeDistances;
        }

        Vector3[] GetFrustum8Point(Camera camera)
        {
            Vector3[] nearCorners = GetFrustumCorners(camera, camera.nearClipPlane);
            Vector3[] farCorners = GetFrustumCorners(camera, camera.farClipPlane);
    
            Vector3[] frustumVertices = new Vector3[8];
            nearCorners.CopyTo(frustumVertices, 0);
            farCorners.CopyTo(frustumVertices, 4);
            return frustumVertices;
        }

        private Vector3[] GetFrustumCorners(Camera camera, float distance)
        {
            Vector3[] frustumCorners = new Vector3[4];

            camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), distance, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);

            for (int i = 0; i < 4; i++)
            {
                frustumCorners[i] = camera.transform.TransformPoint(frustumCorners[i]);
            }
            return frustumCorners;
        }

        public static void RenderShadowmap(CommandBuffer cmd, Camera camera, CameraRenderType renderType, int cascadeIndex)
        {
            if (camera == null || renderType != CameraRenderType.Base)
            {
                return;
            }
            //先只支持3级csm
            if (2 < cascadeIndex)
            {
                return;
            }

            material.SetInt("csmIdx", cascadeIndex);
            switch(cascadeIndex)
            {
                case 0:
                    cmd.DrawProceduralIndirect(Matrix4x4.identity, material, 1, MeshTopology.Triangles, shadowArgsBuffer0, 0);
                    break;
                case 1:
                    cmd.DrawProceduralIndirect(Matrix4x4.identity, material, 1, MeshTopology.Triangles, shadowArgsBuffer1, 0);
                    break;
                case 2:
                    cmd.DrawProceduralIndirect(Matrix4x4.identity, material, 1, MeshTopology.Triangles, shadowArgsBuffer2, 0);
                    break;
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