using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

public class GPUTerrain : MonoBehaviour
{
    public ComputeShader terrainCS;
    int traverseQuadTreeKernel;
    CommandBuffer cmdBuffer;
    ComputeBuffer indirectArgsBuffer;
    ComputeBuffer nodeListABuffer;
    ComputeBuffer nodeListBBuffer;
    ComputeBuffer finalNodeListBuffer;
    ComputeBuffer maxNodeList;

    //预估的
    int maxNodeBufferSize = 200;
    //预估的
    int tempNodeBufferSize = 50;

    const int MAX_LOD = 5;
    const int MAX_LOD_NODE_COUNT = 5;
    Vector3 worldSize = new Vector3(10240,2048,10240);

    Camera mainCamera;
    
    static readonly int cameraPosWSID = Shader.PropertyToID("_CameraPosWS");
    static readonly int lodLevelID = Shader.PropertyToID("_LodLevel");
    static readonly int nodeListAID = Shader.PropertyToID("_NodeListA");
    static readonly int nodeListBID = Shader.PropertyToID("_NodeListB");
    static readonly int finalNodeListID = Shader.PropertyToID("_FinalNodeList");
    
    void Start()
    {
        mainCamera = Camera.main;

        cmdBuffer = new CommandBuffer();

        traverseQuadTreeKernel = terrainCS.FindKernel("TraverseQuadTree");
        nodeListABuffer = new ComputeBuffer(tempNodeBufferSize, 8, ComputeBufferType.Append);
        nodeListBBuffer = new ComputeBuffer(tempNodeBufferSize, 8, ComputeBufferType.Append);
        indirectArgsBuffer = new ComputeBuffer(3, 4, ComputeBufferType.IndirectArguments);
        indirectArgsBuffer.SetData(new uint[]{1, 1, 1});
        finalNodeListBuffer = new ComputeBuffer(maxNodeBufferSize, 12, ComputeBufferType.Append);

        terrainCS.SetBuffer(traverseQuadTreeKernel, "_FinalNodeList", finalNodeListBuffer);

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
    }

    void Update()
    {
        cmdBuffer.Clear();
        cmdBuffer.SetBufferCounterValue(maxNodeList, 25);
        cmdBuffer.SetBufferCounterValue(nodeListABuffer, 0);
        cmdBuffer.SetBufferCounterValue(nodeListBBuffer, 0);
        cmdBuffer.SetBufferCounterValue(finalNodeListBuffer, 0);

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

        /*uint3[] data = new uint3[maxNodeBufferSize];
        finalNodeListBuffer.GetData(data);
        foreach(var node in data){
            Debug.Log(node);
        }*/

        Graphics.ExecuteCommandBuffer(cmdBuffer);
    }

    void OnDisable()
    {
        maxNodeList.Dispose();
        nodeListABuffer.Dispose();
        nodeListBBuffer.Dispose();
        finalNodeListBuffer.Dispose();
        indirectArgsBuffer.Dispose();
        cmdBuffer.Dispose();
    }
}
