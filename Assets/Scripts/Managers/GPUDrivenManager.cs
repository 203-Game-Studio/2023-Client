using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class GPUDrivenManager : MonoBehaviour, IManager
{
    #region Mgr 周期
    public static GPUDrivenManager Instance => GameManager.Instance.GPUDrivenMgr;

    public void OnManagerInit() 
    {
    }

    public void OnManagerUpdate(float deltTime)
    {
    }

    public void OnManagerDestroy()
    {
    }
    #endregion

    public struct RenderObjectData{
        public Transform transform;
        public int guid;
    }

    public struct MeshData
    {
        public bool isLoaded;
        public int offset;
    }

    public static Dictionary<int, RenderObjectData> renderObjects = new Dictionary<int, RenderObjectData>();

    public static Dictionary<string, MeshData> meshData = new Dictionary<string, MeshData>();

    public void AddRenderObject(string meshGuid, Transform transform){
        var insID = transform.gameObject.GetInstanceID();
        bool res = renderObjects.TryAdd(insID, new RenderObjectData());
        if(!res){
            Debug.LogError($"{transform.name}添加出错!");
            return;
        }
        if(!meshData.ContainsKey(meshGuid)){
            Debug.Log($"{meshGuid}未载入，开始加载");
            var data = ClusterizerUtil.LoadMeshDataFromFile("default");
        }
    }

    public void RemoveRenderObject(string meshGuid, Transform transform){
        var insID = transform.gameObject.GetInstanceID();
        if(renderObjects.ContainsKey(insID)){
            renderObjects.Remove(insID);
        }
    }
}
