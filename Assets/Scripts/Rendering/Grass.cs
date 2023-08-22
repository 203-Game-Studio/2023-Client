using System.Collections.Generic;
using UnityEngine;

public class Grass : MonoBehaviour
{
    private static Grass instance;
    public static Grass Instance{
        get{
            return instance;
        }
    }

    //草数量
    [SerializeField]
    private int instanceCount = 10000;
    int cachedInstanceCount = -1;

    //用于读取裁剪后的实例数量
    ComputeBuffer countBuffer;
    uint[] countBufferData = new uint[1] { 0 };
    public int cullResultCount{
        get{
            return (int)countBufferData[0];
        }
    }

    //用于视椎体剔除
    public ComputeShader frustumCullingComputeShader;
    int kernel;

    //矩阵buffer
    ComputeBuffer localToWorldMatrixBuffer;
    //裁剪后的矩阵buffer
    ComputeBuffer cullResult;

    Camera mainCamera;

    private System.Random random = new System.Random();

    private void Awake()
    {
        instance = this;
    }

    void Start() {
        kernel = frustumCullingComputeShader.FindKernel("ViewPortCulling");
        mainCamera = Camera.main;
        cullResult = new ComputeBuffer(instanceCount, sizeof(float) * 16, ComputeBufferType.Append);
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

        localToWorldMatrixBuffer = new ComputeBuffer(instanceCount, 16 * sizeof(float));
        List<Matrix4x4> mats = new List<Matrix4x4>();
        for(int i = 0;i < instanceCount; ++i){
            var upToNormal = Quaternion.FromToRotation(Vector3.up,Vector3.up);
            var positionInTerrian = new Vector3(GetRandomNumber(-10,10),0,GetRandomNumber(-10,10));
            float rot = Random.Range(0,180);
            var localToTerrian = Matrix4x4.TRS(positionInTerrian,  upToNormal * Quaternion.Euler(0,rot,0) ,Vector3.one);
            
            mats.Add(transform.localToWorldMatrix * localToTerrian);
        }
        localToWorldMatrixBuffer.SetData(mats);

        cachedInstanceCount = instanceCount;
    }

    void Update() {
        if(cachedInstanceCount != instanceCount) UpdateBuffers();

        //获取主相机视椎体6个平面
        Vector4[] planes = CullTool.GetFrustumPlane(mainCamera);

        //执行视椎体剔除
        frustumCullingComputeShader.SetVectorArray("planes", planes);
        frustumCullingComputeShader.SetInt("instanceCount", instanceCount);
        frustumCullingComputeShader.SetBuffer(kernel, "localToWorldMatrixBuffer", localToWorldMatrixBuffer);
        cullResult.SetCounterValue(0);
        frustumCullingComputeShader.SetBuffer(kernel, "cullResult", cullResult);
        frustumCullingComputeShader.Dispatch(kernel, 1 + (instanceCount / 640), 1, 1);

        //获取裁剪后的实例数量
        ComputeBuffer.CopyCount(cullResult, countBuffer, 0);
        countBuffer.GetData(countBufferData);
    }
    
    //获取裁剪后的矩阵Buffer
    public ComputeBuffer CullResultBuffer{
        get{
            return cullResult;
        }
    }

    void OnDisable(){
        localToWorldMatrixBuffer?.Release();
        localToWorldMatrixBuffer = null;

        cullResult?.Release();
        cullResult = null;

        countBuffer?.Release();
        countBuffer = null;
    }
}