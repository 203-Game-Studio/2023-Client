using System;
using UnityEngine;

/// <summary>
/// 环境管理器
/// </summary>
public class EnvironmentManager : MonoBehaviour, IManager
{
    public static EnvironmentManager Instance => GameManager.Instance.EnvironmentMgr;

    /////////////////////////////////////////////
    /// 风
    /////////////////////////////////////////////
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

    [Header("风向")]
    [Range(0, 360)]
    public int windAngle = 45;

    /////////////////////////////////////////////
    /// 天空
    /////////////////////////////////////////////
    [SerializeField] private Transform sunTransform = null;
    [SerializeField] private Transform moonTransform = null;
    [SerializeField] private Transform directionLight = null;
    [Header("天空设置")]
    public int windSpeed = 0;

    public void OnManagerInit(){
        //初始化风相关参数
        OnWindChanged();
    }

    public void OnManagerUpdate(float deltTime){
        //TODO:现在方便debug用，后面不能扔update里做，得整成事件触发
        OnWindChanged();
    }

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

        //拿角度算方向
        float x = Mathf.Cos(windAngle*3.14159265359f/180);
        float y = Mathf.Sin(windAngle*3.14159265359f/180);
        Vector4 dir = new(x,0,-y,0);
        Shader.SetGlobalVector("_Wind",             dir);
    }
}