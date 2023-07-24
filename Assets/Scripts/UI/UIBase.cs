using UnityEngine;

//UI层级类型
public enum UILayerType
{
    Top,
    Upper,
    Normal,
    Lower,
    HUD,
}

//UI加载方式
public enum UILoadType { 
    Sync,
    Async
}

public abstract class UIBase
{
    /// <summary>
    /// UI名
    /// </summary>
    protected string uiName;
    public string UIName {
        get { return uiName; }
        set { 
            uiName = value;
        }
    }

    /// <summary>
    /// 是否初始化完毕
    /// </summary>
    protected bool isInited;
    public bool IsInited
    {
        get { return isInited; }
    }

    /// <summary>
    /// 是否缓存这个UI
    /// </summary>
    protected bool cacheUI = false;
    public bool CacheUI
    {
        get { return cacheUI; }
        set
        {
            cacheUI = value;
        }
    }

    /// <summary>
    /// UI的实例
    /// </summary>
    protected GameObject uiGameObject;
    public GameObject UIGameObject {
        get { return uiGameObject; }
        set
        {
            uiGameObject = value;
        }
    }

    /// <summary>
    /// 可见性
    /// </summary>
    protected bool activeSelf = false;
    public bool ActiveSelf {
        get { return activeSelf; } 
        set { 
            activeSelf = value;
            uiGameObject.SetActive(activeSelf);

            if (activeSelf) OnActive();
            else OnDeActive();
        }
    }

    /// <summary>
    /// 层级
    /// </summary>
    protected UILayerType layerType;

    /// <summary>
    /// 加载方式
    /// </summary>
    protected UILoadType loadType;


    protected UIBase(string uiName, UILayerType layerType, UILoadType loadType) {
        this.uiName = uiName;
        this.layerType = layerType;
        this.loadType = loadType;
    }

    /// <summary>
    /// 显示时触发
    /// </summary>
    protected abstract void OnActive();

    /// <summary>
    /// 隐藏时触发
    /// </summary>

    protected abstract void OnDeActive();

    /// <summary>
    /// 初始化时触发
    /// </summary>
    protected abstract void OnInit();

    /// <summary>
    /// 销毁时触发
    /// </summary>
    protected abstract void OnDestory();

    public void InitUI() {
        //todo: 后面改成Addressables方式加载，先写点丑陋的
        if (loadType == UILoadType.Sync)
        {
            GameObject go = GameObject.Instantiate(Resources.Load<GameObject>(uiName));
            go.name = uiName;
            OnLoadFinish(go);
        }
        else { 
            var req = Resources.LoadAsync<GameObject>(uiName);
            req.completed += (AsyncOperation obj) => {
                OnLoadFinish(req.asset as GameObject);
            };
        }
    }

    //加载成功回调
    private void OnLoadFinish(GameObject prefab) {
        if (prefab == null) {
            Debug.LogError($"{uiName}加载失败！");
            return;
        }

        //初始化UI GameObject
        GameObject go = GameObject.Instantiate(prefab);
        go.name = uiName;
        uiGameObject = go;

        //放到对应层级
        SetPanetByLayerType(layerType);

        //重置transform数值
        uiGameObject.transform.localScale = Vector3.one;
        uiGameObject.transform.localPosition = Vector3.zero;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.offsetMin = new Vector2(0.0f, 0.0f);
        rt.offsetMax = new Vector2(0.0f, 0.0f);

        isInited = true;

        OnInit();
    }

    public void DestoryUI(bool forceDestory = false) {
        if (forceDestory || !cacheUI)
        {
            OnDestory();
            GameObject.Destroy(uiGameObject);
        }
        else
        {
            //todo: 扔池子里
        }
        isInited = false;
        activeSelf = false;
    }

    /// <summary>
    /// 设置UI层级
    /// </summary>
    /// <param name="layerType"></param>
    protected void SetPanetByLayerType(UILayerType layerType){
        switch (layerType)
        {
            case UILayerType.Top:
                uiGameObject.transform.SetParent(GM_UI.Instance.TopLayerTransform);
                break;
            case UILayerType.Upper:
                uiGameObject.transform.SetParent(GM_UI.Instance.UpperLayerTransform);
                break;
            case UILayerType.Normal:
                uiGameObject.transform.SetParent(GM_UI.Instance.NormalLayerTransform);
                break;
            case UILayerType.Lower:
                uiGameObject.transform.SetParent(GM_UI.Instance.LowerLayerTransform);
                break;
            case UILayerType.HUD:
                uiGameObject.transform.SetParent(GM_UI.Instance.HUDLayerTransform);
                break;
        }
    }
}
