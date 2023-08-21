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
    public Material instanceMaterial;
    [SerializeField]
    private int _instanceCount = 10000;
    
    public int instanceCount{
        get{
            return _instanceCount;
        }
    }

    int cachedInstanceCount = -1;

    ComputeBuffer argsBuffer;

    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    public ComputeShader compute;

    ComputeBuffer localToWorldMatrixBuffer;

    ComputeBuffer cullResult;

    int kernel;

    Camera mainCamera;

    private System.Random random = new System.Random();

    private void Awake()
    {
        instance = this;
    }

    void Start() {
        kernel = compute.FindKernel("ViewPortCulling");
        mainCamera = Camera.main;
        cullResult = new ComputeBuffer(instanceCount, sizeof(float) * 16, ComputeBufferType.Append);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        UpdateBuffers();
    }

    void OnDisable(){
        localToWorldMatrixBuffer?.Release();
        localToWorldMatrixBuffer = null;

        cullResult?.Release();
        cullResult = null;

        argsBuffer?.Release();
        argsBuffer = null;
    }

    public float GetRandomNumber(float minimum, float maximum)
    { 
        return (float)random.NextDouble() * (maximum - minimum) + minimum;
    }

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

        // Indirect args
        if(GrassUtil.unitMesh != null) {
            args[0] = (uint)GrassUtil.unitMesh.GetIndexCount(0);
            args[2] = (uint)GrassUtil.unitMesh.GetIndexStart(0);
            args[3] = (uint)GrassUtil.unitMesh.GetBaseVertex(0);
        } else {
            args[0] = args[1] = args[2] = args[3] = 0;
        }
        argsBuffer.SetData(args);

        cachedInstanceCount = instanceCount;
    }

    void Update() {
        if(cachedInstanceCount != instanceCount)
            UpdateBuffers();

        Vector4[] planes = CullTool.GetFrustumPlane(mainCamera);

        compute.SetBuffer(kernel, "input", localToWorldMatrixBuffer);
        cullResult.SetCounterValue(0);
        compute.SetBuffer(kernel, "cullresult", cullResult);
        compute.SetInt("instanceCount", instanceCount);
        compute.SetVectorArray("planes", planes);
        
        compute.Dispatch(kernel, 1 + (instanceCount / 640), 1, 1);
        instanceMaterial.SetBuffer("_LocalToWorldMats", cullResult);

        ComputeBuffer.CopyCount(cullResult, argsBuffer, sizeof(uint));

        Graphics.DrawMeshInstancedIndirect(GrassUtil.unitMesh, 0, instanceMaterial, new Bounds(Vector3.zero, new Vector3(200.0f, 200.0f, 200.0f)), argsBuffer);
    }
    
    public ComputeBuffer LocalToWorldMatrixBuffer{
        get{
            if(localToWorldMatrixBuffer != null){
                return localToWorldMatrixBuffer;
            }
            UpdateBuffers();
            return localToWorldMatrixBuffer;
        }
    }
    
    public static readonly int matsID = Shader.PropertyToID("_LocalToWorldMats");
    public void UpdateMaterialProperties(){
        materialPropertyBlock.SetBuffer(matsID, LocalToWorldMatrixBuffer);
    }

    private MaterialPropertyBlock _materialBlock;

    public MaterialPropertyBlock materialPropertyBlock{
        get{
            if(_materialBlock == null){
                _materialBlock = new MaterialPropertyBlock();
            }
            return _materialBlock;
        }
    }
}