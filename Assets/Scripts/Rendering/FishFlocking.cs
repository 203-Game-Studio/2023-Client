using UnityEngine;

public class FishFlocking : MonoBehaviour
{
    public float speed = 5.0f;
    public float viewRadius = 3.0f;
    public float separationRadius =  0.5f;
    public float separationWeight =  0.5f;
    public float cohesionWeight =  0.5f;
    public float alignmentWeight =  0.5f;
    public float targetWeight =  0.5f;
    public int count = 100;
    public ComputeShader fishFlcokingCS;
    public Mesh fishMesh;
    public Material fishMat;
    public bool debug;

    public Transform target;
    public Transform obstacle;
    
    private ComputeBuffer fishDataBuffer;
    private int kernel;

    struct FishData{
        public Vector3 position;
        public Vector3 direction;
    }
    private FishData[] fishDatas;

    int groupSizeX;
    
    void Start(){
        kernel = fishFlcokingCS.FindKernel("FishFlocking");
        groupSizeX = Mathf.CeilToInt(count / 256.0f);
        int num = groupSizeX * 256;

        fishDatas = new FishData[count];
        for(int idx = 0; idx < count; ++idx){
            Vector3 offset = new Vector3(Random.Range(-256,256),Random.Range(-256,256),Random.Range(-256,256));
            fishDatas[idx] = new FishData();
            fishDatas[idx].position = transform.position + offset;
            fishDatas[idx].direction = Vector3.zero;

            //fishGOs[idx] = GameObject.Instantiate(prefab);
        }

        fishDataBuffer = new ComputeBuffer(num, 6 * sizeof(float));
        fishDataBuffer.SetData(fishDatas);
        fishFlcokingCS.SetBuffer(kernel, "fishDataBuffer", fishDataBuffer);
        fishFlcokingCS.SetInt("fishCount", count);
    }

    void Update(){
        fishFlcokingCS.SetFloat("deltaTime", Time.deltaTime);
        fishFlcokingCS.SetFloat("speed", speed);
        fishFlcokingCS.SetFloat("viewRadius", viewRadius);
        fishFlcokingCS.SetFloat("separationRadius", separationRadius);
        fishFlcokingCS.SetVector("target", target.position);
        //fishFlcokingCS.SetVector("obstacle", obstacle.position);
        fishFlcokingCS.SetFloat("separationWeight", separationWeight);
        fishFlcokingCS.SetFloat("cohesionWeight", cohesionWeight);
        fishFlcokingCS.SetFloat("alignmentWeight", alignmentWeight);
        fishFlcokingCS.SetFloat("targetWeight", targetWeight);

        fishFlcokingCS.Dispatch(kernel, groupSizeX, 1, 1);
        if(debug){
            fishDataBuffer.GetData(fishDatas);
        }

        fishMat.SetBuffer("_Fishes", fishDataBuffer);
        var bounds = new Bounds(Vector3.zero, Vector3.one * 512);
        Graphics.DrawMeshInstancedProcedural(fishMesh, 0, fishMat, bounds, count);
    }

    void OnDrawGizmos(){
        if(!Application.isPlaying || !debug) return;
        for(int idx = 0; idx < count; ++idx){
            Gizmos.DrawRay(fishDatas[idx].position, fishDatas[idx].direction);
        }
    }
}