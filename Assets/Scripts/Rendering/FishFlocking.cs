using UnityEngine;

public class FishFlocking : MonoBehaviour
{
    public float speed = 5.0f;
    public float viewRadius = 3.0f;
    public float separationRadius =  0.5f;
    public int count = 100;
    public ComputeShader fishFlcokingCS;
    public GameObject prefab;

    public Transform target;
    public Transform obstacle;
    
    private ComputeBuffer fishDataBuffer;
    private int kernel;

    struct FishData{
        public Vector3 position;
        public Vector3 direction;
    }
    private FishData[] fishDatas;
    private GameObject[] fishGOs;

    int groupSizeX;
    
    void Start(){
        kernel = fishFlcokingCS.FindKernel("FishFlocking");
        groupSizeX = Mathf.CeilToInt(count / 256.0f);
        int num = groupSizeX * 256;

        fishGOs = new GameObject[count];
        fishDatas = new FishData[count];
        for(int idx = 0; idx < count; ++idx){
            Vector3 offset = new Vector3(Random.Range(-2,2),Random.Range(-2,2),Random.Range(-2,2));
            fishDatas[idx] = new FishData();
            fishDatas[idx].position = transform.position + offset;
            fishDatas[idx].direction = Vector3.zero;

            fishGOs[idx] = GameObject.Instantiate(prefab);
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
        fishFlcokingCS.SetVector("obstacle", obstacle.position);

        fishFlcokingCS.Dispatch(kernel, groupSizeX, 1, 1);
        fishDataBuffer.GetData(fishDatas);

        for(int idx = 0; idx < count; ++idx){
            fishGOs[idx].transform.position = fishDatas[idx].position;
            if (fishDatas[idx].direction.sqrMagnitude > 0.00001)
            {
                Vector3 up = Vector3.Slerp(transform.up, fishDatas[idx].direction, speed * Time.deltaTime);
                fishGOs[idx].transform.right = up;
            }
        }
    }
}