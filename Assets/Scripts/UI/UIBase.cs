using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

//UI层级类型
public enum EUILayerType
{
    Top,
    Upper,
    Normal,
    Lower,
    HUD,
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
    protected EUILayerType layerType;


    protected UIBase(string uiName, EUILayerType layerType, bool cacheUI = false) {
        this.uiName = uiName;
        this.layerType = layerType;
        this.cacheUI = cacheUI;
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
        //加载UI资源
        AssetManager.GetInstance(uiName, OnLoadFinish, cacheUI);
    }

    //加载成功回调
    private void OnLoadFinish(GameObject go) {
        if (go == null) {
            Debug.LogError($"{uiName}加载失败！");
            return;
        }

        //初始化UI GameObject
        uiGameObject = go;
        uiGameObject.name = uiName;

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

    public void DestoryUI(bool forceDestory = false)
    {
        OnDestory();
        isInited = false;
        activeSelf = false;
        AssetManager.ReleaseInstance(uiGameObject, uiName, !forceDestory && cacheUI, 1);
    }

    /// <summary>
    /// 设置UI层级
    /// </summary>
    /// <param name="layerType"></param>
    protected void SetPanetByLayerType(EUILayerType layerType){
        switch (layerType)
        {
            case EUILayerType.Top:
                uiGameObject.transform.SetParent(UIManager.Instance.TopLayerTransform);
                break;
            case EUILayerType.Upper:
                uiGameObject.transform.SetParent(UIManager.Instance.UpperLayerTransform);
                break;
            case EUILayerType.Normal:
                uiGameObject.transform.SetParent(UIManager.Instance.NormalLayerTransform);
                break;
            case EUILayerType.Lower:
                uiGameObject.transform.SetParent(UIManager.Instance.LowerLayerTransform);
                break;
            case EUILayerType.HUD:
                uiGameObject.transform.SetParent(UIManager.Instance.HUDLayerTransform);
                break;
        }
    }
}
