using System.Collections.Generic;
using UnityEngine;

public class FishFlocking : MonoBehaviour
{
    public float speed = 5.0f;
    public float viewRadius = 3.0f;
    public float targetRadius = 10.0f;
    public float separationRadius =  0.5f;
    public float separationWeight =  0.5f;
    public float cohesionWeight =  0.5f;
    public float alignmentWeight =  0.5f;
    public float targetWeight =  0.5f;
    public float obstacleWeight =  0.5f;
    public int count = 100;
    public ComputeShader fishFlcokingCS;
    public Mesh fishMesh;
    public Material fishMat;
    public bool debug;
    public bool spatialIndexing;

    public List<Transform> obstacleList;
    
    private ComputeBuffer fishDataBuffer;
    private ComputeBuffer targets;
    private ComputeBuffer obstacles;
    private ComputeBuffer fishOffset;
    private ComputeBuffer fishIndex;
    private ComputeBuffer fishCount;
    private int flockingKernel;
    private int indexKernel;
    private int countKernel;
    private int cellCount;
    private int cellCount2;
    private int cellCount3;

    struct FishData{
        public Vector3 position;
        public float speed;
        //public float placeholder;//占位 用来对齐内存
        public Vector3 direction;
        public uint targetIdx;
    }
    private FishData[] fishDatas;
    private uint[] counts;
    private uint[] countsAfter;
    private uint[] offsetbuffer;

    int groupSizeX;
    List<Vector3> targetList;
    Vector4[] obstacleSphereArray;
    
    void Start(){
        if(spatialIndexing){
            flockingKernel = fishFlcokingCS.FindKernel("FishFlockingUseSpatialIndexing");
        }else{
            flockingKernel = fishFlcokingCS.FindKernel("FishFlocking");
        }
        groupSizeX = Mathf.CeilToInt(count / 256.0f);
        int num = groupSizeX * 256;

        targetList = new List<Vector3>(){
            new Vector3(1,0,0) * 20.0f,
            new Vector3(0.707f,0,0.707f) * 20.0f,
            new Vector3(0,0,1) * 20.0f,
            new Vector3(-0.707f,0,0.707f) * 20.0f,
            new Vector3(-1,0,0) * 20.0f,
            new Vector3(-0.707f,0,-0.707f) * 20.0f,
            new Vector3(0,0,-1) * 20.0f,
            new Vector3(0.707f,0,-0.707f) * 20.0f,
        };
        targets = new ComputeBuffer(targetList.Count, 3 * sizeof(float));
        targets.SetData(targetList.ToArray());
        fishFlcokingCS.SetBuffer(flockingKernel, "targets", targets);
        fishFlcokingCS.SetInt("targetCount", targetList.Count);

        obstacles = new ComputeBuffer(obstacleList.Count, 4 * sizeof(float));
        fishFlcokingCS.SetInt("obstacleCount", obstacleList.Count);
        obstacleSphereArray = new Vector4[obstacleList.Count];
        for(int i = 0; i < obstacleList.Count; ++i){
            obstacleSphereArray[i] = new Vector4(obstacleList[i].position.x, obstacleList[i].position.y,
                obstacleList[i].position.z, obstacleList[i].localScale.x * obstacleList[i].localScale.x * 1.2f);
        }
        obstacles.SetData(obstacleSphereArray);
        fishFlcokingCS.SetBuffer(flockingKernel, "obstacles", obstacles);

        fishDatas = new FishData[count];
        for(int idx = 0; idx < count; ++idx){
            Vector3 forward = new Vector3(Random.Range(-1.0f,1.0f), 0, Random.Range(-1.0f,1.0f));
            forward = forward.normalized;
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(up, forward).normalized;
            fishDatas[idx] = new FishData();
            fishDatas[idx].position = forward * 20.0f;
            fishDatas[idx].position.y = Random.Range(-3.0f,3.0f);
            fishDatas[idx].direction = Vector3.zero;
            uint targetIdx = 0;
            float dis = 15.0f;
            for(int i = 0; i < targetList.Count; ++i){
                float curDis = (targetList[i]-forward).magnitude;
                if(curDis < dis){
                    dis = curDis;
                    targetIdx = (uint)i;
                }
            }
            fishDatas[idx].targetIdx = targetIdx;
            fishDatas[idx].speed = Random.Range(speed-1.0f,speed+1.0f);
        }

        fishDataBuffer = new ComputeBuffer(num, 32);
        fishDataBuffer.SetData(fishDatas);
        fishFlcokingCS.SetBuffer(flockingKernel, "fishDataBuffer", fishDataBuffer);
        fishFlcokingCS.SetInt("count", count);
        
        if(spatialIndexing){
            indexKernel = fishFlcokingCS.FindKernel("SpatialIndexing");
            countKernel = fishFlcokingCS.FindKernel("CellCounts");
            fishFlcokingCS.SetBuffer(indexKernel, "fishDataBuffer", fishDataBuffer);
            fishFlcokingCS.SetBuffer(countKernel, "fishDataBuffer", fishDataBuffer);

            cellCount = Mathf.CeilToInt(100 / viewRadius);
            cellCount2 = cellCount * cellCount;
            cellCount3 = cellCount2 * cellCount;
            fishFlcokingCS.SetInt("cellCount", cellCount);
            fishFlcokingCS.SetInt("cellCount2", cellCount2);
            fishFlcokingCS.SetInt("cellCount3", cellCount3);
            counts = new uint[cellCount3];
            countsAfter = new uint[cellCount3];
            offsetbuffer = new uint[cellCount3];
            for(int idx = 0; idx < cellCount3; ++idx){
                counts[idx] = 0;
            }
            fishCount = new ComputeBuffer(cellCount3, sizeof(uint));
            fishIndex = new ComputeBuffer(num, sizeof(uint));
            fishOffset = new ComputeBuffer(cellCount3, sizeof(uint));
            fishFlcokingCS.SetBuffer(flockingKernel, "fishCount", fishCount);
            fishFlcokingCS.SetBuffer(flockingKernel, "fishIndex", fishIndex);
            fishFlcokingCS.SetBuffer(flockingKernel, "fishOffset", fishOffset);
            fishFlcokingCS.SetBuffer(indexKernel, "fishIndex", fishIndex);
            fishFlcokingCS.SetBuffer(indexKernel, "fishOffset", fishOffset);
            fishFlcokingCS.SetBuffer(countKernel, "fishCount", fishCount);
        }
        
    }

    void Update(){
        fishFlcokingCS.SetFloat("deltaTime", Time.deltaTime);
        fishFlcokingCS.SetFloat("viewRadius2", viewRadius * viewRadius);
        fishFlcokingCS.SetFloat("targetRadius2", targetRadius * targetRadius);
        fishFlcokingCS.SetFloat("separationRadius2", separationRadius * separationRadius);
        fishFlcokingCS.SetFloat("separationWeight", separationWeight);
        fishFlcokingCS.SetFloat("cohesionWeight", cohesionWeight);
        fishFlcokingCS.SetFloat("alignmentWeight", alignmentWeight);
        fishFlcokingCS.SetFloat("targetWeight", targetWeight);
        fishFlcokingCS.SetFloat("obstacleWeight", obstacleWeight);
        
        for(int i = 0; i < obstacleList.Count; ++i){
            obstacleSphereArray[i] = new Vector4(obstacleList[i].position.x, obstacleList[i].position.y,
                obstacleList[i].position.z, obstacleList[i].localScale.x * obstacleList[i].localScale.x * 1.2f);
        }
        obstacles.SetData(obstacleSphereArray);

        if(spatialIndexing){
            fishFlcokingCS.SetFloat("viewRadius",  viewRadius);
            fishCount.SetData(counts);
            fishFlcokingCS.Dispatch(countKernel, groupSizeX, 1, 1);
            fishCount.GetData(countsAfter);

            uint offset = 0;
            for(int i = 0;i < cellCount3;++i){
                offsetbuffer[i] = offset;
                offset += countsAfter[i];
            }
            fishOffset.SetData(offsetbuffer);
            fishFlcokingCS.Dispatch(indexKernel, groupSizeX, 1, 1);
        }
        
        
        fishFlcokingCS.Dispatch(flockingKernel, groupSizeX, 1, 1);
        
        if(debug){
            fishDataBuffer.GetData(fishDatas);
        }

        fishMat.SetBuffer("_Fishes", fishDataBuffer);
        var bounds = new Bounds(Vector3.zero, Vector3.one * 512);
        Graphics.DrawMeshInstancedProcedural(fishMesh, 0, fishMat, bounds, count);
    }

    void OnDrawGizmos(){
        if(!Application.isPlaying || !debug) return;
        for(int i = 0; i < targetList.Count; ++i){
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(targetList[i], 1.0f);
        }
        for(int idx = 0; idx < count; ++idx){
            Gizmos.color = Color.green;
            Gizmos.DrawRay(fishDatas[idx].position, fishDatas[idx].direction);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(fishDatas[idx].position, targetList[(int)fishDatas[idx].targetIdx]);
        }
    }
}