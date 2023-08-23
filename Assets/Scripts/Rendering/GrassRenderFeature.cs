using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrassRenderFeature : ScriptableRendererFeature
{
    private GrassRenderPass pass = null;

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

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(renderingData.cameraData.renderType == CameraRenderType.Base){
            renderer.EnqueuePass(pass);
        }
    }
    
    public override void Create()
    {
        pass ??= new(settings);
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

        public GrassRenderPass(Settings settings){
            Clear();
            this.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            this.settings = settings;
            grassCullingComputeShader = settings.grassCullingComputeShader;

            kernel = grassCullingComputeShader.FindKernel("GrassCulling");
            mainCamera = Camera.main;
            cullResult = new ComputeBuffer(settings.grassCount, sizeof(float) * 16, ComputeBufferType.Append);
            countBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.IndirectArguments);
            UpdateBuffers();
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
                float rot = Random.Range(0,180);
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
                Matrix4x4 vpMatrix = GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, false) * mainCamera.worldToCameraMatrix;
                grassCullingComputeShader.SetMatrix("vpMatrix", vpMatrix);
                grassCullingComputeShader.SetBuffer(kernel, "localToWorldMatrixBuffer", localToWorldMatrixBuffer);
                cullResult.SetCounterValue(0);
                grassCullingComputeShader.SetBuffer(kernel, "cullResult", cullResult);
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
                if(settings.grassCount <= 0) return;
                settings.grassMaterial.SetBuffer("_LocalToWorldMats", cullResult);
                cmd.DrawMeshInstancedProcedural(Grass.GrassMesh, 0, settings.grassMaterial, 0, count);
                context.ExecuteCommandBuffer(cmd);
            }
            finally{
                CommandBufferPool.Release(cmd);
            }
        }

        void Clear(){
            localToWorldMatrixBuffer?.Release();
            localToWorldMatrixBuffer = null;

            cullResult?.Release();
            cullResult = null;

            countBuffer?.Release();
            countBuffer = null;
        }

        ~GrassRenderPass(){
            Clear();
        }
    }
}