using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
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
    private int _grassCount = 10000;
    public Material material{
        get{
            return _material;
        }
    }
    private ComputeBuffer _grassBuffer;
    private System.Random random = new System.Random();

    [ContextMenu("ForceRebuildGrassInfoBuffer")]
    private void ForceUpdateGrassBuffer(){
        if(_grassBuffer != null){
            _grassBuffer.Dispose();
            _grassBuffer = null;
        }
        UpdateMaterialProperties();
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

    public float GetRandomNumber(float minimum, float maximum)
    { 
        return (float)random.NextDouble() * (maximum - minimum) + minimum;
    }
    
    public ComputeBuffer grassBuffer{
        get{
            if(_grassBuffer != null){
                return _grassBuffer;
            }
            var matrix = transform.localToWorldMatrix;
            List<Matrix4x4> mats = new List<Matrix4x4>();
            for(int i = 0;i < _grassCount; ++i){
                var upToNormal = Quaternion.FromToRotation(Vector3.up,Vector3.up);
                var positionInTerrian = new Vector3(GetRandomNumber(-5,5),0,GetRandomNumber(-5,5));
                float rot = Random.Range(0,180);
                var localToTerrian = Matrix4x4.TRS(positionInTerrian,  upToNormal * Quaternion.Euler(0,rot,0) ,Vector3.one);
                
                mats.Add(transform.localToWorldMatrix * localToTerrian);
            }
           
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