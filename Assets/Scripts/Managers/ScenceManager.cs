using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场景管理器
/// </summary>
public class ScenceManager : MonoBehaviour, IManager
{
    public Material RobotMaterial;
    public Material RobotEyesMaterial;
    public void OnManagerInit() {}
    
    public void OnManagerUpdate(float deltTime){}
    public void OnManagerDestroy(){}
}
