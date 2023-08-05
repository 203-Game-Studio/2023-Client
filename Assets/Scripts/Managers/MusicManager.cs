using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 音乐管理器
/// </summary>

public class MusicConfig
{
    public static string BGM1 = "";
    public static void Init()
    {
        
    }
}


public class MusicManager : MonoBehaviour, IManager
{
    public static MusicManager Instance => GameManager.Instance.MusicMgr;
    
    //总 音量
    public float AllVolume;
    //BGM 音量
    public float BGMVolume;
    public void OnManagerInit()
    {
        MusicConfig.Init();
    }
    
    public void OnManagerUpdate(float deltTime){}
    public void OnManagerDestroy(){}
}
