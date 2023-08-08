using System;
using UnityEngine;

/// <summary>
/// 环境管理器
/// </summary>
public class EnvironmentManager : MonoBehaviour, IManager
{
    public static EnvironmentManager Instance => GameManager.Instance.EnvironmentMgr;

    [Header("树干风设置")]
    [Range(0f, 5f)]
    public float baseWindPower = 3f;
    public float baseWindSpeed = 1f;

    [Header("偏移风设置")][Range(0f, 10f)]
    public float burstsPower = 0.5f;
    public float burstsSpeed = 5f;
    public float burstsScale = 10f;

    [Header("微风设置")]
    [Range(0f, 1f)]
    public float microPower = 0.1f;
    public float microSpeed = 1f;
    public float microFrequency = 3f;

    public void OnManagerInit(){
        //初始化风相关参数
        OnWindChanged();
    }

    public void OnManagerUpdate(float deltTime){}

    public void OnManagerDestroy(){ }

    //风改变时触发
    public void OnWindChanged(){
        Shader.SetGlobalFloat("_WindPower",         baseWindPower);
        Shader.SetGlobalFloat("_WindSpeed",         baseWindSpeed);
        Shader.SetGlobalFloat("_WindBurstsPower",   burstsPower);
        Shader.SetGlobalFloat("_WindBurstsSpeed",   burstsSpeed);
        Shader.SetGlobalFloat("_WindBurstsScale",   burstsScale);
        Shader.SetGlobalFloat("_MicroPower",        microPower);
        Shader.SetGlobalFloat("_MicroSpeed",        microSpeed);
        Shader.SetGlobalFloat("_MicroFrequency",    microFrequency);
    }
}