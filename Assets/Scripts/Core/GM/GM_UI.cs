using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI管理器
/// </summary>
public class GM_UI : MonoBehaviour, IManager
{
    public static GM_UI Instance => GameManager.Instance.gmUI;

    //五个UI层级
    private Transform topLayerTransform;
    private Transform upperLayerTransform;
    private Transform normalLayerTransform;
    private Transform lowerLayerTransform;
    private Transform hudLayerTransform;
    public Transform TopLayerTransform { 
        get { return topLayerTransform; }
    }
    public Transform UpperLayerTransform
    {
        get { return upperLayerTransform; }
    }
    public Transform NormalLayerTransform
    {
        get { return normalLayerTransform; }
    }
    public Transform LowerLayerTransform
    {
        get { return lowerLayerTransform; }
    }
    public Transform HUDLayerTransform
    {
        get { return hudLayerTransform; }
    }

    //UI相机
    private Camera uiCamera;
    public Camera UICamera => uiCamera;

    //UI根节点
    public GameObject uiRoot => this.gameObject;

    //UI实例查找表
    private static Dictionary<Type, UIBase> uiMap;

    //绑定Layer及初始化UI查找表
    public void OnManagerInit()
    {
        topLayerTransform = transform.Find("UICanvas/TopLayer");
        upperLayerTransform = transform.Find("UICanvas/UpperLayer");
        normalLayerTransform = transform.Find("UICanvas/NormalLayer");
        lowerLayerTransform = transform.Find("UICanvas/LowerLayer");
        hudLayerTransform = transform.Find("UICanvas/HUDLayer");

        uiMap = new Dictionary<Type, UIBase>
        {
            { typeof(UI_Login), new UI_Login() }
        };
    }

    public void OnManagerUpdate(float deltTime){}

    //强制销毁所有UI
    public void OnManagerDestroy()
    {
        topLayerTransform = null;
        upperLayerTransform = null;
        normalLayerTransform = null;
        lowerLayerTransform = null;
        hudLayerTransform = null;

        foreach (var ui in uiMap) {
            ui.Value.DestoryUI(true);
        }
        uiMap.Clear();
    }

    /// <summary>
    /// 获取UI
    /// </summary>
    /// <param name="uiName"></param>
    /// <returns></returns>

    private static T GetUI<T>() where T : UIBase
    {
        UIBase ui = null;
        if (uiMap.TryGetValue(typeof(T), out ui))
        {
            return ui as T;
        }
        return null;
    }

    /// <summary>
    /// 打开UI
    /// </summary>
    /// <param name="uiName"></param>
    /// <returns></returns>
    public static T OpenUI<T>() where T : UIBase
    {
        UIBase ui = GetUI<T>();
        if (ui == null)
        {
            Debug.LogError($"{nameof(T)}未找到！");
            return null;
        }

        if (!ui.IsInited)
        {
            ui.InitUI();
        }

        return ui as T;
    }

    /// <summary>
    /// 关闭UI
    /// </summary>
    /// <param name="uiName"></param>
    /// <returns></returns>
    public static bool CloseUI<T>() where T : UIBase
    {
        UIBase ui = GetUI<T>();
        if (ui == null)
        {
            Debug.LogError($"{nameof(T)}未找到！");
            return false;
        }

        if (ui.IsInited)
        {
            ui.DestoryUI();
        }

        return true;
    }

    /// <summary>
    /// 显示UI
    /// </summary>
    /// <param name="uiName"></param>
    /// <returns></returns>
    public static void ActiveUI<T>() where T : UIBase
    {
        UIBase ui = GetUI<T>();
        if (ui == null)
        {
            Debug.LogError($"{nameof(T)}未找到！");
        }

        if (ui.IsInited && !ui.ActiveSelf)
        {
            ui.ActiveSelf = true;
        }
    }

    /// <summary>
    /// 隐藏UI
    /// </summary>
    /// <param name="uiName"></param>
    /// <returns></returns>
    public static void DeactiveUI<T>() where T : UIBase
    {
        UIBase ui = GetUI<T>();
        if (ui == null)
        {
            Debug.LogError($"{nameof(T)}未找到！");
        }

        if (ui.IsInited && ui.ActiveSelf)
        {
            ui.ActiveSelf = false;
        }
    }
}
