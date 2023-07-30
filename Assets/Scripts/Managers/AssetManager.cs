using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 资源管理器
/// </summary>
public class AssetManager : MonoBehaviour, IManager
{
    public static AssetManager Instance => GameManager.Instance.AssetMgr;

    private static InstancePool _instancePool;

    public void OnManagerInit()
    {
        //生成挂载节点
        Transform attchNode = new GameObject("AttachNode").transform;
        attchNode.SetParent(gameObject.transform);
        //隐藏挂载节点 这样后面挂载到他下面的都会隐藏
        attchNode.gameObject.SetActive(false);

        _instancePool = new InstancePool();
        _instancePool.SceneAttachNode = attchNode;
    }

    public void OnManagerUpdate(float deltTime){}

    public void OnManagerDestroy()
    {
        _instancePool.Release();
    }
    
    /// <summary>
    /// 获取一个资源实例
    /// </summary>
    /// <param name="assetID"></param>
    /// <param name="callback"></param>
    /// <param name="usePool"></param>
    public static void GetInstance(string assetID, Action<GameObject> callback, bool usePool = false) {
        if (usePool){
            _instancePool.Get(assetID, callback);
        }
        else {
            Addressables.InstantiateAsync(assetID).Completed += (handle) =>
            {
                callback?.Invoke(handle.Result);
            };
        }
    }

    /// <summary>
    /// 释放一个资源实例
    /// </summary>
    /// <param name="go"></param>
    /// <param name="assetID"></param>
    /// <param name="usePool"></param>
    /// <param name="maxSaveNum"></param>
    public static void ReleaseInstance(GameObject go, string assetID = "", bool usePool = false, int maxSaveNum = 100) {
        if (usePool)
        {
            _instancePool.Recycle(assetID, go, maxSaveNum);
        }
        else
        {
            Addressables.ReleaseInstance(go);
        }
    }
}

/// <summary>
/// 资源池
/// </summary>
public class InstancePool {
    //所有池的容器
    private Dictionary<string, InstancePoolUnit> _pools = new Dictionary<string, InstancePoolUnit>();
    
    /// <summary>
    /// 池挂载节点
    /// </summary>
    public Transform SceneAttachNode = null;

    /// <summary>
    /// 获取一个资源
    /// </summary>
    /// <param name="callback"></param>
    public void Get(string assetID, Action<GameObject> callback)
    {
        if (string.IsNullOrEmpty(assetID))
        {
            callback?.Invoke(null);
            return;
        }

        InstancePoolUnit unit = null;
        if (!_pools.ContainsKey(assetID))
        {
            unit = new InstancePoolUnit();
            unit.Init(assetID, SceneAttachNode);
            _pools[assetID] = unit;
        }
        else
        {
            unit = _pools[assetID];
        }
        unit.Get(callback);
    }

    /// <summary>
    /// 回收一个资源
    /// </summary>
    /// <param name="assetID"></param>
    /// <param name="go"></param>
    /// <param name="maxSaveNum"></param>
    public void Recycle(string assetID, GameObject go, int maxSaveNum)
    {
        if (go == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(assetID))
        {
            Addressables.ReleaseInstance(go);
        }
        else
        {
            _pools.TryGetValue(assetID, out var unit);
            if (unit != null)
            {
                bool release = unit.Size() >= maxSaveNum;
                unit.Recycle(go, release);
            }
            else
            {
                Addressables.ReleaseInstance(go);
            }
        }
    }

    /// <summary>
    /// 释放一个池
    /// </summary>
    public void ReleaseUnit(string assetID)
    {
        _pools.TryGetValue(assetID, out var unit);
        if (unit != null)
        {
            unit.Release();
            _pools.Remove(assetID);
        }
    }

    /// <summary>
    /// 释放所有池
    /// </summary>
    public void Release()
    {
        foreach (var pool in _pools)
        {
            pool.Value.Release();
        }
        _pools.Clear();
    }
}

public class InstancePoolUnit
{
    //资源ID
    private string _assetID;

    // 挂载节点
    private Transform _sceneNode = null;

    // 缓存池
    private Queue<GameObject> _pool = new Queue<GameObject>();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="assetID"></param>
    /// <param name="sceneNode"></param>
    public void Init(string assetID, Transform sceneNode)
    {
        _assetID = assetID;
        _sceneNode = sceneNode;
    }

    /// <summary>
    /// 获取池中数量
    /// </summary>
    /// <returns></returns>
    public int Size() { 
        return _pool.Count;
    }

    /// <summary>
    /// 获取一个资源
    /// </summary>
    /// <param name="callback"></param>
    public void Get(System.Action<GameObject> callback)
    {
        if (callback != null)
        {
            if (_pool.Count > 0)
            {
                callback.Invoke(_pool.Dequeue());
            }
            else
            {
                Addressables.InstantiateAsync(_assetID, _sceneNode).Completed += (handle) =>
                {
                    if (handle.Status != AsyncOperationStatus.Succeeded)
                    {
                        Debug.LogError($"{_assetID}加载失败！");
                        return;
                    }
                    callback.Invoke(handle.Result);
                };
            }
        }
    }

    /// <summary>
    /// 回收一个资源
    /// </summary>
    /// <param name="go"></param>
    /// <param name="release"></param>
    public void Recycle(GameObject go, bool release = false)
    {
        if (go == null) return;

        if (release)
        {
            Addressables.ReleaseInstance(go);
        }
        else
        {
            go.transform.SetParent(_sceneNode);
            _pool.Enqueue(go);
        }
    }

    /// <summary>
    /// 释放池
    /// </summary>
    public void Release()
    {
        while (_pool.Count > 0)
        {
            var go = _pool.Dequeue();
            Addressables.ReleaseInstance(go);
        }
        _sceneNode = null;
        _assetID = null;
        _pool.Clear();
    }
}