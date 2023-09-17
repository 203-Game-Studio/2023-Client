using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using System.Collections.Generic;

public class GPUTerrain : MonoBehaviour
{
    public ComputeShader terrainCS;
    int traverseQuadTreeKernel;
    int buildPatchesKernel;
    CommandBuffer cmdBuffer;
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

    Camera mainCamera;
    
    static readonly int cameraPosWSID = Shader.PropertyToID("_CameraPosWS");
    static readonly int lodLevelID = Shader.PropertyToID("_LodLevel");
    static readonly int nodeListAID = Shader.PropertyToID("_NodeListA");
    static readonly int nodeListBID = Shader.PropertyToID("_NodeListB");

    static Mesh _patchMesh;
    static Mesh patchMesh{
        get{
            if(!_patchMesh){
                _patchMesh = CreatePlaneMesh(16);
            }
            return _patchMesh;
        }
    }
    
    Material terrainMat;
    void Start()
    {
        mainCamera = Camera.main;

        cmdBuffer = new CommandBuffer();

        traverseQuadTreeKernel = terrainCS.FindKernel("TraverseQuadTree");
        buildPatchesKernel = terrainCS.FindKernel("BuildPatches");
        nodeListABuffer = new ComputeBuffer(tempNodeBufferSize, 8, ComputeBufferType.Append);
        nodeListBBuffer = new ComputeBuffer(tempNodeBufferSize, 8, ComputeBufferType.Append);
        indirectArgsBuffer = new ComputeBuffer(3, 4, ComputeBufferType.IndirectArguments);
        indirectArgsBuffer.SetData(new uint[]{1, 1, 1});
        finalNodeListBuffer = new ComputeBuffer(maxNodeBufferSize, 12, ComputeBufferType.Append);
        nodeDescriptorsBuffer = new ComputeBuffer((int)(MAX_NODE_ID + 1), 4);
        culledPatchBuffer = new ComputeBuffer(maxNodeBufferSize * 64, 3 * 4, ComputeBufferType.Append);
        patchIndirectArgs = new ComputeBuffer(5, 4, ComputeBufferType.IndirectArguments);
        patchIndirectArgs.SetData(new uint[]{patchMesh.GetIndexCount(0),0,0,0,0});

        terrainCS.SetBuffer(traverseQuadTreeKernel, "_FinalNodeList", finalNodeListBuffer);
        terrainCS.SetBuffer(traverseQuadTreeKernel, "_NodeDescriptors", nodeDescriptorsBuffer);
        terrainCS.SetBuffer(buildPatchesKernel, "_CulledPatchList", culledPatchBuffer);
        terrainCS.SetBuffer(buildPatchesKernel, "_RenderNodeList", finalNodeListBuffer);

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
    }

    void Update()
    {
        cmdBuffer.Clear();
        cmdBuffer.SetBufferCounterValue(maxNodeList, 25);
        cmdBuffer.SetBufferCounterValue(nodeListABuffer, 0);
        cmdBuffer.SetBufferCounterValue(nodeListBBuffer, 0);
        cmdBuffer.SetBufferCounterValue(finalNodeListBuffer, 0);
        cmdBuffer.SetBufferCounterValue(culledPatchBuffer, 0);

        cmdBuffer.SetComputeVectorParam(terrainCS, cameraPosWSID, mainCamera.transform.position);

        cmdBuffer.CopyCounterValue(maxNodeList, indirectArgsBuffer, 0);
        ComputeBuffer nodeListA = nodeListABuffer;
        ComputeBuffer nodeListB = nodeListBBuffer;
        for(var lod = MAX_LOD; lod >= 0; --lod){
            cmdBuffer.SetComputeIntParam(terrainCS, lodLevelID, lod);
            if(lod == MAX_LOD){
                cmdBuffer.SetComputeBufferParam(terrainCS, traverseQuadTreeKernel, nodeListAID, maxNodeList);
            }else{
                cmdBuffer.SetComputeBufferParam(terrainCS, traverseQuadTreeKernel, nodeListAID, nodeListA);
            }
            cmdBuffer.SetComputeBufferParam(terrainCS, traverseQuadTreeKernel, nodeListBID, nodeListB);
            cmdBuffer.DispatchCompute(terrainCS, traverseQuadTreeKernel, indirectArgsBuffer,0);
            cmdBuffer.CopyCounterValue(nodeListB, indirectArgsBuffer, 0);
            ComputeBuffer tempBuf = nodeListA;
            nodeListA = nodeListB;
            nodeListB = tempBuf;
        }

        //生成Patch
        cmdBuffer.CopyCounterValue(finalNodeListBuffer, indirectArgsBuffer, 0);
        cmdBuffer.DispatchCompute(terrainCS, buildPatchesKernel, indirectArgsBuffer,0);
        cmdBuffer.CopyCounterValue(culledPatchBuffer, patchIndirectArgs,4);

        Graphics.ExecuteCommandBuffer(cmdBuffer);

        Graphics.DrawMeshInstancedIndirect(patchMesh, 0, terrainMat, 
            new Bounds(Vector3.zero,Vector3.one * 10240), patchIndirectArgs);
    }

    void OnDisable()
    {
        maxNodeList.Dispose();
        nodeListABuffer.Dispose();
        nodeListBBuffer.Dispose();
        finalNodeListBuffer.Dispose();
        indirectArgsBuffer.Dispose();
        nodeDescriptorsBuffer.Dispose();
        culledPatchBuffer.Dispose();
        cmdBuffer.Dispose();
    }

    static Mesh CreatePlaneMesh(int size){
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
}
