using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrassRenderFeature : ScriptableRendererFeature
{
    private GrassRenderPass grassPass = null;
    private DepthGeneratorPass depthPass = null;

    [System.Serializable]
    public class Settings
    {
        //草材质
        public Material grassMaterial;
        //草数量
        public int grassCount = 10000;
        //裁剪shader
        public ComputeShader grassCullingComputeShader;
    }
    public Settings settings;

    private RenderTexture depthTexture;
    static int _depthTextureSize = 0;
    public static int depthTextureSize {
        get {
            if(_depthTextureSize == 0)
                _depthTextureSize = Mathf.NextPowerOfTwo(Mathf.Max(Screen.width, Screen.height));
            return _depthTextureSize;
        }
    }
    const RenderTextureFormat depthTextureFormat = RenderTextureFormat.RHalf;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Preview) {
            return;
        }
        InitDepthTexture();

        depthPass.Setup(depthTexture);
        renderer.EnqueuePass(depthPass);

        grassPass.Setup(depthTexture);
        renderer.EnqueuePass(grassPass);
    }

    public override void Create()
    {
        grassPass ??= new(settings);
        depthPass ??= new();
    }

    void InitDepthTexture() {
        if(depthTexture != null) return;
        depthTexture = new RenderTexture(depthTextureSize, depthTextureSize, 0, depthTextureFormat);
        depthTexture.autoGenerateMips = false;
        depthTexture.useMipMap = true;
        depthTexture.filterMode = FilterMode.Point;
        depthTexture.Create();
    }

    protected override void Dispose(bool disposing) {
        //depthTexture?.Release();
        depthTexture = null;
    }

    public class DepthGeneratorPass : ScriptableRenderPass {
        RenderTexture depthTexture;
        Material depthTextureMaterial;
        int depthTextureShaderID;
        private const string bufferName = "Depth Gen Buffer";

        public DepthGeneratorPass() {
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
            depthTextureShaderID = Shader.PropertyToID("_CameraDepthTexture");
            depthTextureMaterial = new Material(Shader.Find("John/DepthGeneratorShader"));
        }

        public void Setup(RenderTexture depthTexture) {
            ConfigureInput(ScriptableRenderPassInput.Depth);
            ConfigureClear(ClearFlag.None, Color.white);
            this.depthTexture = depthTexture;
            //这个调用提示过时了，后面改成新的
            ConfigureTarget(this.depthTexture);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            if(!Application.isPlaying || renderingData.cameraData.cameraType != CameraType.Game) return;
            var cmd = CommandBufferPool.Get(bufferName);
            try{
                ///////////////////////////////
                ///生成深度图的mipmap
                ///////////////////////////////
                int width = depthTexture.width;
                int mipmapLevel = 0;
                RenderTexture currentRenderTexture = null;
                RenderTexture preRenderTexture = null;
                while(width > 8) {
                    currentRenderTexture = RenderTexture.GetTemporary(width, width, 0, depthTextureFormat);
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
                }
            catch(Exception e){
                Debug.LogException(e);
            }
            finally{
                CommandBufferPool.Release(cmd);
            }
        }
    }

    public class GrassRenderPass : ScriptableRenderPass
    {
        Settings settings;

        //用于读取裁剪后的实例数量
        ComputeBuffer countBuffer;
        uint[] countBufferData = new uint[1] { 0 };

        //用于视椎体剔除
        private ComputeShader grassCullingComputeShader;
        int kernel;

        //矩阵buffer
        ComputeBuffer localToWorldMatrixBuffer;
        //裁剪后的矩阵buffer
        ComputeBuffer cullResult;

        Camera mainCamera;
        private System.Random random = new System.Random();
        int cachedInstanceCount = -1;

        private const string bufferName = "GrassBuffer";

        private RenderTexture depthTexture;

        public GrassRenderPass(Settings settings){
            this.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            this.settings = settings;
            Clear();
            
            grassCullingComputeShader = settings.grassCullingComputeShader;
            kernel = grassCullingComputeShader.FindKernel("GrassCulling");

            mainCamera = Camera.main;
            cullResult = new ComputeBuffer(settings.grassCount, sizeof(float) * 16, ComputeBufferType.Append);
            countBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.IndirectArguments);
            UpdateBuffers();
        }

        public void Setup(RenderTexture depthTexture){
            this.depthTexture = depthTexture;
        }

        public float GetRandomNumber(float minimum, float maximum)
        { 
            return (float)random.NextDouble() * (maximum - minimum) + minimum;
        }

        //更新矩阵buffer和数量buffer
        void UpdateBuffers() {
            if(localToWorldMatrixBuffer != null)
                localToWorldMatrixBuffer.Release();

            localToWorldMatrixBuffer = new ComputeBuffer(settings.grassCount, 16 * sizeof(float));
            List<Matrix4x4> mats = new List<Matrix4x4>();
            for(int i = 0;i < settings.grassCount; ++i){
                var upToNormal = Quaternion.FromToRotation(Vector3.up,Vector3.up);
                var positionInTerrian = new Vector3(GetRandomNumber(-10,10),0,GetRandomNumber(-10,10));
                float rot = UnityEngine.Random.Range(0,180);
                var localToTerrian = Matrix4x4.TRS(positionInTerrian,  upToNormal * Quaternion.Euler(0,rot,0) ,Vector3.one);

                mats.Add(localToTerrian);
            }
            localToWorldMatrixBuffer.SetData(mats);

            cachedInstanceCount = settings.grassCount;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData){
            if(!Application.isPlaying) return;
            var cmd = CommandBufferPool.Get(bufferName);
            try{

                if(cachedInstanceCount != settings.grassCount) UpdateBuffers();
                if(settings.grassCount <= 0) return;

                ///////////////////////////////
                /// 剔除
                ///////////////////////////////
                grassCullingComputeShader.SetInt("instanceCount", settings.grassCount);
                grassCullingComputeShader.SetInt("depthTextureSize", depthTextureSize);
                Matrix4x4 vpMatrix = GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, false) * mainCamera.worldToCameraMatrix;
                grassCullingComputeShader.SetMatrix("vpMatrix", vpMatrix);
                grassCullingComputeShader.SetBuffer(kernel, "localToWorldMatrixBuffer", localToWorldMatrixBuffer);
                cullResult.SetCounterValue(0);
                grassCullingComputeShader.SetBuffer(kernel, "cullResult", cullResult);
                grassCullingComputeShader.SetTexture(kernel, "hizTexture", depthTexture);
                grassCullingComputeShader.Dispatch(kernel, 1 + (settings.grassCount / 640), 1, 1);

                //获取裁剪后的实例数量
                ComputeBuffer.CopyCount(cullResult, countBuffer, 0);
                countBuffer.GetData(countBufferData);

                ///////////////////////////////
                /// 绘制
                ///////////////////////////////
                cmd.Clear();
                int count = (int)countBufferData[0];
                //可能出现0数量，比如朝天看
                if(count <= 0) return;
                settings.grassMaterial.SetTexture("_hizTexture", depthTexture);
                settings.grassMaterial.SetBuffer("_LocalToWorldMats", cullResult);
                cmd.DrawMeshInstancedProcedural(Grass.GrassMesh, 0, settings.grassMaterial, 0, count);
                context.ExecuteCommandBuffer(cmd);
            }
            catch(Exception e){
                Debug.LogException(e);
            }
            finally{
                CommandBufferPool.Release(cmd);
            }
        }

        ~GrassRenderPass(){
            Clear();
        }

        public void Clear(){
            localToWorldMatrixBuffer?.Release();
            localToWorldMatrixBuffer = null;

            cullResult?.Release();
            cullResult = null;

            countBuffer?.Release();
            countBuffer = null;
        }
    }
}