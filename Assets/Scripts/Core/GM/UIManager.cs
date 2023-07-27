using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI管理器
/// </summary>
public class UIManager : MonoBehaviour, IManager
{
    public static UIManager Instance => GameManager.Instance.UIMgr;

    //五个UI层级
    private Transform _topLayerTransform;
    private Transform _upperLayerTransform;
    private Transform _normalLayerTransform;
    private Transform _lowerLayerTransform;
    private Transform _hudLayerTransform;
    public Transform TopLayerTransform { 
        get { return _topLayerTransform; }
    }
    public Transform UpperLayerTransform
    {
        get { return _upperLayerTransform; }
    }
    public Transform NormalLayerTransform
    {
        get { return _normalLayerTransform; }
    }
    public Transform LowerLayerTransform
    {
        get { return _lowerLayerTransform; }
    }
    public Transform HUDLayerTransform
    {
        get { return _hudLayerTransform; }
    }

    //UI相机
    private Camera _uiCamera;
    public Camera UICamera => _uiCamera;

    //UI根节点
    public GameObject UIRoot => this.gameObject;

    //UI实例查找表
    private static Dictionary<Type, UIBase> _uiMap;

    //绑定Layer及初始化UI查找表
    public void OnManagerInit()
    {
        _topLayerTransform = transform.Find("UICanvas/TopLayer");
        _upperLayerTransform = transform.Find("UICanvas/UpperLayer");
        _normalLayerTransform = transform.Find("UICanvas/NormalLayer");
        _lowerLayerTransform = transform.Find("UICanvas/LowerLayer");
        _hudLayerTransform = transform.Find("UICanvas/HUDLayer");

        _uiMap = new Dictionary<Type, UIBase>
        {
            { typeof(UILogin), new UILogin() }
        };
    }

    public void OnManagerUpdate(float deltTime){}

    //强制销毁所有UI
    public void OnManagerDestroy()
    {
        _topLayerTransform = null;
        _upperLayerTransform = null;
        _normalLayerTransform = null;
        _lowerLayerTransform = null;
        _hudLayerTransform = null;

        foreach (var ui in _uiMap) {
            ui.Value.DestoryUI(true);
        }
        _uiMap.Clear();
    }

    /// <summary>
    /// 获取UI
    /// </summary>
    /// <param name="uiName"></param>
    /// <returns></returns>

    private static T GetUI<T>() where T : UIBase
    {
        UIBase ui = null;
        if (_uiMap.TryGetValue(typeof(T), out ui))
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
