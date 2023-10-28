using System.Collections.Generic;
using UnityEngine;

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
        public string meshName;
    }

    public struct MeshInfo
    {
        public int offset;
        public int count;
    }

    public static Dictionary<int, RenderObjectData> renderObjectDic = new Dictionary<int, RenderObjectData>();

    public static Dictionary<string, MeshInfo> meshInfoDic = new Dictionary<string, MeshInfo>();

    public static bool renderObjectDicIsDirty = false;
    public static bool meshInfoDicIsDirty = false;

    public static void AddRenderObject(string meshName, Transform transform){
        var insID = transform.gameObject.GetInstanceID();
        var renderObjectData = new RenderObjectData();
        renderObjectData.meshName = meshName;
        renderObjectData.transform = transform;
        bool res = renderObjectDic.TryAdd(insID, renderObjectData);
        if(!res){
            Debug.LogError($"{transform.name}添加出错!");
            return;
        }
        renderObjectDicIsDirty = true;
        /*if(!meshInfoDic.ContainsKey(meshName)){
            Debug.Log($"{meshName}未载入，开始加载");
            var data = ClusterizerUtil.LoadMeshDataFromFile("default");
            MeshInfo meshInfo = new MeshInfo();
            meshInfo.isLoaded = true;
            meshInfo.meshData = data;
            meshInfoDic.Add(meshName, meshInfo);
            meshInfoDicIsDirty = true;
        }*/
    }

    Matrix4x4[] instanceData = new Matrix4x4[1];
    public Matrix4x4[] BuildInstanceData(){
        if(renderObjectDicIsDirty) instanceData = new Matrix4x4[renderObjectDic.Count];
        int idx = 0;
        foreach(var obj in renderObjectDic){
            instanceData[idx++] = obj.Value.transform.localToWorldMatrix;
        }
        return instanceData;
    }

    public static void RemoveRenderObject(string meshGuid, Transform transform){
        var insID = transform.gameObject.GetInstanceID();
        if(renderObjectDic.ContainsKey(insID)){
            renderObjectDic.Remove(insID);
            renderObjectDicIsDirty = true;
        }
    }
}
