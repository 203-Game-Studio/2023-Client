using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TerrainFeature : ScriptableRendererFeature
{
    private TerrainRenderPass terrainPass = null;
    private DepthGeneratorPass depthPass = null;

    [System.Serializable]
    public class Settings
    {
        public Texture2D heightMap;

        public Texture2D normalMap;

        public Texture2D[] _minMaxHeightMaps;

        public ComputeShader terrainCS;
    }
    public Settings settings;

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

        terrainPass.Setup(depthTexture);
        renderer.EnqueuePass(terrainPass);
    }

    public override void Create()
    {
        terrainPass ??= new(settings);
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
        //terrainPass?.Dispose();
    }

    public class DepthGeneratorPass : ScriptableRenderPass {
        RTHandle depthTexture;
        Material depthTextureMaterial;
        int depthTextureShaderID;
        const string bufferName = "Depth Gen Buffer";

        public DepthGeneratorPass() {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
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
            var cmd = CommandBufferPool.Get(bufferName);
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

    public class TerrainRenderPass : ScriptableRenderPass
    {
        private Settings settings;

        private const string bufferName = "Terrain Pass";

        private RTHandle depthTexture;

        private ComputeShader terrainCS;
        int traverseQuadTreeKernel;
        int buildPatchesKernel;
        
        ComputeBuffer indirectArgsBuffer;
        ComputeBuffer nodeListABuffer;
        ComputeBuffer nodeListBBuffer;
        ComputeBuffer finalNodeListBuffer;
        ComputeBuffer nodeDescriptorsBuffer;
        ComputeBuffer culledPatchBuffer;
        ComputeBuffer maxNodeList;
        ComputeBuffer patchIndirectArgs;

        //预估的
        int maxNodeBufferSize = 200;
        //预估的
        int tempNodeBufferSize = 50;

        const int MAX_LOD = 5;
        const int MAX_LOD_NODE_COUNT = 5;
        const uint MAX_NODE_ID = 34124;
        Vector3 worldSize = new Vector3(10240,2048,10240);
        Vector4[] cameraFrustumPlanesVec = new Vector4[6];
        Plane[] cameraFrustumPlanes = new Plane[6];

        Camera mainCamera;

        static readonly int cameraPosWSID = Shader.PropertyToID("_CameraPosWS");
        static readonly int lodLevelID = Shader.PropertyToID("_LodLevel");
        static readonly int nodeListAID = Shader.PropertyToID("_NodeListA");
        static readonly int nodeListBID = Shader.PropertyToID("_NodeListB");
        static readonly int vpMatrixID = Shader.PropertyToID("_VPMatrix");
        static readonly int depthTextureSizeID = Shader.PropertyToID("_DepthTextureSize");
        static readonly int hiZTextureID = Shader.PropertyToID("_HiZTexture");

        private Mesh _patchMesh;
        private Mesh patchMesh{
            get{
                if(!_patchMesh){
                    _patchMesh = CreatePlaneMesh(16);
                }
                return _patchMesh;
            }
        }

        RenderTexture _minMaxHeightMap;
        RenderTexture minMaxHeightMap{
            get{
                if(!_minMaxHeightMap){
                    _minMaxHeightMap = CreateRenderTextureWithMipTextures(settings._minMaxHeightMaps,
                        RenderTextureFormat.RG32);
                }
                return _minMaxHeightMap;
            }
        }

        Material terrainMat;

        public TerrainRenderPass(Settings settings){
            this.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            this.settings = settings;
            
            terrainCS = settings.terrainCS;
            mainCamera = Camera.main;
            
            traverseQuadTreeKernel = terrainCS.FindKernel("TraverseQuadTree");
            buildPatchesKernel = terrainCS.FindKernel("BuildPatches");

            nodeListABuffer = new ComputeBuffer(tempNodeBufferSize, 8, ComputeBufferType.Append);
            nodeListBBuffer = new ComputeBuffer(tempNodeBufferSize, 8, ComputeBufferType.Append);
            indirectArgsBuffer = new ComputeBuffer(3, 4, ComputeBufferType.IndirectArguments);
            indirectArgsBuffer.SetData(new uint[]{1, 1, 1});
            finalNodeListBuffer = new ComputeBuffer(maxNodeBufferSize, 12, ComputeBufferType.Append);
            nodeDescriptorsBuffer = new ComputeBuffer((int)(MAX_NODE_ID + 1), 4);
            culledPatchBuffer = new ComputeBuffer(maxNodeBufferSize * 64, 5 * 4, ComputeBufferType.Append);
            patchIndirectArgs = new ComputeBuffer(5, 4, ComputeBufferType.IndirectArguments);
            patchIndirectArgs.SetData(new uint[]{patchMesh.GetIndexCount(0),0,0,0,0});

            terrainCS.SetBuffer(traverseQuadTreeKernel, "_FinalNodeList", finalNodeListBuffer);
            terrainCS.SetTexture(traverseQuadTreeKernel, "_MinMaxHeightTexture", minMaxHeightMap);
            terrainCS.SetBuffer(traverseQuadTreeKernel, "_NodeDescriptors", nodeDescriptorsBuffer);
            terrainCS.SetBuffer(buildPatchesKernel, "_CulledPatchList", culledPatchBuffer);
            terrainCS.SetBuffer(buildPatchesKernel, "_RenderNodeList", finalNodeListBuffer);
            terrainCS.SetTexture(buildPatchesKernel, "_MinMaxHeightTexture", minMaxHeightMap);
            terrainCS.SetInt("_BoundsHeightRedundance", 5);
            terrainCS.SetVector("_WorldSize", worldSize);

            float wSize = worldSize.x;
            int nodeCount = MAX_LOD_NODE_COUNT;
            Vector4[] nodeParams = new Vector4[MAX_LOD + 1];
            for(var lod = MAX_LOD; lod >= 0; --lod){
                var nodeSize = wSize / nodeCount;
                var patchExtent = nodeSize / 16;
                var sectorCountPerNode = (int)Mathf.Pow(2, lod);
                nodeParams[lod] = new Vector4(nodeSize, patchExtent, nodeCount, sectorCountPerNode);
                nodeCount *= 2;
            }
            terrainCS.SetVectorArray("_NodeParams", nodeParams);

            int[] nodeIDOffsets = new int[(MAX_LOD + 1) * 4];
            int nodeIdOffset = 0;
            for(int lod = MAX_LOD; lod >= 0; --lod){
                nodeIDOffsets[lod * 4] = nodeIdOffset;
                nodeIdOffset += (int)(nodeParams[lod].z * nodeParams[lod].z);
            }
            terrainCS.SetInts("_NodeIDOffsets", nodeIDOffsets);

            maxNodeList = new ComputeBuffer(MAX_LOD_NODE_COUNT * MAX_LOD_NODE_COUNT, 8, ComputeBufferType.Append);
            uint2[] datas = new uint2[MAX_LOD_NODE_COUNT * MAX_LOD_NODE_COUNT];
            var index = 0;
            for(uint i = 0; i < MAX_LOD_NODE_COUNT; i ++){
                for(uint j = 0; j < MAX_LOD_NODE_COUNT; j ++){
                    datas[index] = new uint2(i,j);
                    index ++;
                }
            }
            maxNodeList.SetData(datas);
            
            terrainMat = new Material(Shader.Find("John/Terrain"));
            terrainMat.SetBuffer("PatchList", culledPatchBuffer);
            terrainMat.SetTexture("_HeightMap", settings.heightMap);
            terrainMat.SetTexture("_NormalMap", settings.normalMap);
            terrainMat.SetVector("_WorldSize", worldSize);
            terrainMat.SetMatrix("_WorldToNormalMapMatrix",Matrix4x4.Scale(worldSize).inverse);
        }

        public void Setup(RTHandle depthTexture){
            this.depthTexture = depthTexture;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData){
            if(!Application.isPlaying) return;
            var cmd = CommandBufferPool.Get(bufferName);
            using (new ProfilingScope(cmd, profilingSampler)){
                //cmd.Clear();
                cmd.SetBufferCounterValue(maxNodeList, 25);
                cmd.SetBufferCounterValue(nodeListABuffer, 0);
                cmd.SetBufferCounterValue(nodeListBBuffer, 0);
                cmd.SetBufferCounterValue(finalNodeListBuffer, 0);
                cmd.SetBufferCounterValue(culledPatchBuffer, 0);

                cmd.SetComputeVectorParam(terrainCS, cameraPosWSID, mainCamera.transform.position);

                cmd.CopyCounterValue(maxNodeList, indirectArgsBuffer, 0);
                ComputeBuffer nodeListA = nodeListABuffer;
                ComputeBuffer nodeListB = nodeListBBuffer;
                for(var lod = MAX_LOD; lod >= 0; --lod){
                    cmd.SetComputeIntParam(terrainCS, lodLevelID, lod);
                    if(lod == MAX_LOD){
                        cmd.SetComputeBufferParam(terrainCS, traverseQuadTreeKernel, nodeListAID, maxNodeList);
                    }else{
                        cmd.SetComputeBufferParam(terrainCS, traverseQuadTreeKernel, nodeListAID, nodeListA);
                    }
                    cmd.SetComputeBufferParam(terrainCS, traverseQuadTreeKernel, nodeListBID, nodeListB);
                    cmd.DispatchCompute(terrainCS, traverseQuadTreeKernel, indirectArgsBuffer,0);
                    cmd.CopyCounterValue(nodeListB, indirectArgsBuffer, 0);
                    ComputeBuffer tempBuf = nodeListA;
                    nodeListA = nodeListB;
                    nodeListB = tempBuf;
                }

                //生成Patch
                terrainCS.SetInt(depthTextureSizeID, depthTextureSize);
                terrainCS.SetTexture(buildPatchesKernel, hiZTextureID, depthTexture);
                Matrix4x4 vpMatrix = GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, false) * mainCamera.worldToCameraMatrix;
                terrainCS.SetMatrix(vpMatrixID, vpMatrix);

                cmd.CopyCounterValue(finalNodeListBuffer, indirectArgsBuffer, 0);
                cmd.DispatchCompute(terrainCS, buildPatchesKernel, indirectArgsBuffer,0);
                cmd.CopyCounterValue(culledPatchBuffer, patchIndirectArgs,4);

                cmd.DrawMeshInstancedIndirect(patchMesh, 0, terrainMat, 0, patchIndirectArgs);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }
        }

        public void Dispose(){
            maxNodeList?.Dispose();
            nodeListABuffer?.Dispose();
            nodeListBBuffer?.Dispose();
            finalNodeListBuffer?.Dispose();
            indirectArgsBuffer?.Dispose();
            nodeDescriptorsBuffer?.Dispose();
            culledPatchBuffer?.Dispose();
        }

        ~TerrainRenderPass(){
            Dispose();
        }

        Mesh CreatePlaneMesh(int size){
            var mesh = new Mesh();

            var sizePerGrid = 0.5f;
            var totalMeterSize = size * sizePerGrid;
            var gridCount = size * size;
            var triangleCount = gridCount * 2;

            var vOffset = - totalMeterSize * 0.5f;

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            float uvStrip = 1f / size;
            for(var z = 0; z <= size;z ++){
                for(var x = 0; x <= size; x ++){
                    vertices.Add(new Vector3(vOffset + x * 0.5f,0,vOffset + z * 0.5f));
                    uvs.Add(new Vector2(x * uvStrip,z * uvStrip));
                }
            }
            mesh.SetVertices(vertices);
            mesh.SetUVs(0,uvs);

            int[] indices = new int[triangleCount * 3];

            for(var gridIndex = 0; gridIndex < gridCount ; gridIndex ++){
                var offset = gridIndex * 6;
                var vIndex = (gridIndex / size) * (size + 1) + (gridIndex % size);

                indices[offset] = vIndex;
                indices[offset + 1] = vIndex + size + 1;
                indices[offset + 2] = vIndex + 1;
                indices[offset + 3] = vIndex + 1; 
                indices[offset + 4] = vIndex + size + 1;
                indices[offset + 5] = vIndex + size + 2;
            }
            mesh.SetIndices(indices,MeshTopology.Triangles,0);
            mesh.UploadMeshData(false);
            return mesh;
        }

        RenderTexture CreateRenderTextureWithMipTextures(Texture2D[] mipmaps,RenderTextureFormat format){
            var mip0 = mipmaps[0];
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(mip0.width,
                mip0.height, format, 0, mipmaps.Length);
            descriptor.autoGenerateMips = false;
            descriptor.useMipMap = true;
            RenderTexture rt = new RenderTexture(descriptor);
            rt.filterMode = mip0.filterMode;
            rt.Create();
            for(var i = 0; i < mipmaps.Length; ++i){
                Graphics.CopyTexture(mipmaps[i], 0, 0, rt, 0, i);
            }
            return rt;
        }
    }
}