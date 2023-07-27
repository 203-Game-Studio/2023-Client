using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI管理器
/// </summary>
public class AssetManager : MonoBehaviour, IManager
{
    public static AssetManager Instance => GameManager.Instance.AssetMgr;

    public void OnManagerInit()
    {
    }

    public void OnManagerUpdate(float deltTime){}

    //强制销毁所有UI
    public void OnManagerDestroy()
    {
    }
}
