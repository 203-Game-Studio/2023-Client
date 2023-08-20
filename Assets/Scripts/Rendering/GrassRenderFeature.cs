using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrassRenderFeature : ScriptableRendererFeature
{
    private GrassRenderPass pass = null;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(renderingData.cameraData.renderType == CameraRenderType.Base){
            renderer.EnqueuePass(pass);
        }
    }
    
    public override void Create()
    {
        pass ??= new();
    }

    public class GrassRenderPass : ScriptableRenderPass
    {
        public int instanceCount = 100000;
        public Mesh instanceMesh;
        public Material instanceMaterial;
        public int subMeshIndex = 0;

        int cachedInstanceCount = -1;
        int cachedSubMeshIndex = -1;
        ComputeBuffer argsBuffer;
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

        public ComputeShader compute;
        ComputeBuffer localToWorldMatrixBuffer;
        ComputeBuffer cullResult;
        int kernel;
        Camera mainCamera;

        public GrassRenderPass(){
            this.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            /*kernel = compute.FindKernel("ViewPortCulling");
            mainCamera = Camera.main;
            cullResult = new ComputeBuffer(instanceCount, sizeof(float) * 16, ComputeBufferType.Append);
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

            UpdateBuffers();*/
        }

        void UpdateBuffers() {
            if(localToWorldMatrixBuffer != null)
                localToWorldMatrixBuffer.Release();

            localToWorldMatrixBuffer = new ComputeBuffer(instanceCount, 16 * sizeof(float));
            List<Matrix4x4> localToWorldMatrixs = new List<Matrix4x4>();
            for(int i = 0; i < instanceCount; i++) {
                float angle = Random.Range(0.0f, Mathf.PI * 2.0f);
                float distance = Random.Range(20.0f, 100.0f);
                float height = Random.Range(-2.0f, 2.0f);
                float size = Random.Range(0.05f, 0.25f);
                Vector4 position = new Vector4(Mathf.Sin(angle) * distance, height, Mathf.Cos(angle) * distance, size);
                localToWorldMatrixs.Add(Matrix4x4.TRS(position, Quaternion.identity, new Vector3(size, size, size)));
            }
            localToWorldMatrixBuffer.SetData(localToWorldMatrixs);

            // Indirect args
            if(instanceMesh != null) {
                args[0] = (uint)instanceMesh.GetIndexCount(subMeshIndex);
                args[2] = (uint)instanceMesh.GetIndexStart(subMeshIndex);
                args[3] = (uint)instanceMesh.GetBaseVertex(subMeshIndex);
            } else {
                args[0] = args[1] = args[2] = args[3] = 0;
            }
            argsBuffer.SetData(args);

            cachedInstanceCount = instanceCount;
            cachedSubMeshIndex = subMeshIndex;
        }

        private const string bufferName = "GrassBuffer";

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData){
            /*Vector4[] planes = CullTool.GetFrustumPlane(mainCamera);

            compute.SetBuffer(kernel, "input", localToWorldMatrixBuffer);
            cullResult.SetCounterValue(0);
            compute.SetBuffer(kernel, "cullresult", cullResult);
            compute.SetInt("instanceCount", instanceCount);
            compute.SetVectorArray("planes", planes);

            compute.Dispatch(kernel, 1 + (instanceCount / 640), 1, 1);
            instanceMaterial.SetBuffer("positionBuffer", cullResult);
    
            ComputeBuffer.CopyCount(cullResult, argsBuffer, sizeof(uint));*/

            //Graphics.DrawMeshInstancedIndirect(instanceMesh, subMeshIndex, instanceMaterial, new Bounds(Vector3.zero, new Vector3(200.0f, 200.0f, 200.0f)), argsBuffer);

            var cmd = CommandBufferPool.Get(bufferName);
            try{
                cmd.Clear();
                var index = 0;
                //获取所有草块 逐个调用DrawMeshInstancedProcedural
                foreach(var grassTerrian in Grass.actives){
                    if(!grassTerrian || !grassTerrian.material){
                        continue;
                    }
                    grassTerrian.UpdateMaterialProperties();
                    cmd.DrawMeshInstancedProcedural(GrassUtil.unitMesh, 0, grassTerrian.material, 0,
                        grassTerrian.grassCount, grassTerrian.materialPropertyBlock);
                    ++index;
                }
                context.ExecuteCommandBuffer(cmd);
            }
            finally{
                CommandBufferPool.Release(cmd);
            }
        }

        private void OnDisable()
        {
            localToWorldMatrixBuffer?.Release();
            localToWorldMatrixBuffer = null;

            cullResult?.Release();
            cullResult = null;

            argsBuffer?.Release();
            argsBuffer = null;
        }
    }
}