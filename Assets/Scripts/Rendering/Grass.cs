using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
public class Grass : MonoBehaviour
{
    private static HashSet<Grass> _actives = new HashSet<Grass>();
    public static IReadOnlyCollection<Grass> actives{
        get{
            return _actives;
        }
    }
    [SerializeField]
    private Material _material;
    [SerializeField]
    private int _grassCountPerMeter = 100;
    public Material material{
        get{
            return _material;
        }
    }
    private int _seed;
    private ComputeBuffer _grassBuffer;
    private int _grassCount;

    [SerializeField]
    private int _size = 10;
    [SerializeField]
    private bool IsPlane = false;

    [ContextMenu("RebuildMesh")]
    private void RebuildMesh(){
        var meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateMesh(_size, transform);
    }
    [ContextMenu("ForceRebuildGrassInfoBuffer")]
    private void ForceUpdateGrassBuffer(){
        if(_grassBuffer != null){
            _grassBuffer.Dispose();
            _grassBuffer = null;
        }
        UpdateMaterialProperties();
    }

    private void Awake() {
        if(IsPlane) this.RebuildMesh();
        _seed = System.Guid.NewGuid().GetHashCode();
    }
    void OnEnable(){
        _actives.Add(this);
    }
    void OnDisable(){
        _actives.Remove(this);
        if(_grassBuffer != null){
            _grassBuffer.Dispose();
            _grassBuffer = null;
        }
    }
    public int grassCount{
        get{
            return _grassCount;
        }
    }
    private static Mesh CreateMesh(int size = 100, Transform transform = null){
        var mesh = new Mesh();
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        for(var x = 0; x <= size; x ++){
            for(var z = 0; z <= size; z ++){
                var height = 0;//Mathf.PerlinNoise(x / 10f,z/10f) * 5;
                var v = new Vector3(x - transform.localScale.x * size * 0.5f,height,z - transform.localScale.z * size * 0.5f);
                vertices.Add(v);
            }
        }
        for(var x = 0; x < size; x ++){
            for(var z = 0; z < size; z ++){
                var i1 = x * (size + 1) + z;
                var i2 = (x + 1) * (size + 1) + z;
                var i3 = x * (size + 1) + z + 1;
                var i4 = (x + 1) * (size + 1) + z + 1;
                indices.Add(i1);
                indices.Add(i3);
                indices.Add(i2);
                indices.Add(i2);
                indices.Add(i3);
                indices.Add(i4);
            }
        }
        mesh.SetVertices(vertices);
        mesh.SetIndices(indices,MeshTopology.Triangles,0,true);
        mesh.RecalculateNormals();
        mesh.UploadMeshData(false);
        return mesh;
    }
    
    //这部分后面要扔进compute shader去算
    public ComputeBuffer grassBuffer{
        get{
            if(_grassBuffer != null){
                return _grassBuffer;
            }
            var filter = GetComponent<MeshFilter>();
            var terrianMesh = filter.sharedMesh;
            var matrix = transform.localToWorldMatrix;
            var grassIndex = 0;
            List<Matrix4x4> mats = new List<Matrix4x4>();
            Random.InitState(_seed);
            var indices = terrianMesh.triangles;
            var vertices = terrianMesh.vertices;
            for(var j = 0; j < indices.Length / 3; j ++){
                var index1 = indices[j * 3];
                var index2 = indices[j * 3 + 1];
                var index3 = indices[j * 3 + 2];
                var v1 = vertices[index1];
                var v2 = vertices[index2];
                var v3 = vertices[index3];
                //面得到法向
                var normal = GrassUtil.GetFaceNormal(v1,v2,v3);
                //计算up到faceNormal的旋转四元数
                var upToNormal = Quaternion.FromToRotation(Vector3.up,normal);
                //三角面积
                var arena = GrassUtil.GetAreaOfTriangle(v1,v2,v3);
                //计算在该三角面中，需要种植的数量
                var countPerTriangle = Mathf.Max(1,_grassCountPerMeter * arena);
                for(var i = 0; i < countPerTriangle; i ++){
                    
                    var positionInTerrian = GrassUtil.RandomPointInsideTriangle(v1,v2,v3);
                    float rot = Random.Range(0,180);
                    var localToTerrian = Matrix4x4.TRS(positionInTerrian,  upToNormal * Quaternion.Euler(0,rot,0) ,Vector3.one);
                    
                    mats.Add(transform.localToWorldMatrix * localToTerrian);
                    grassIndex ++;
                }
            }
           
            _grassCount = grassIndex;
            _grassBuffer = new ComputeBuffer(_grassCount,64);
            _grassBuffer.SetData(mats);
            return _grassBuffer;
        }
    }
    private MaterialPropertyBlock _materialBlock;
    
    public static readonly int matsID = Shader.PropertyToID("_LocalToWorldMats");
    public void UpdateMaterialProperties(){
        materialPropertyBlock.SetBuffer(matsID, grassBuffer);
    }
    public MaterialPropertyBlock materialPropertyBlock{
        get{
            if(_materialBlock == null){
                _materialBlock = new MaterialPropertyBlock();
            }
            return _materialBlock;
        }
    }
}