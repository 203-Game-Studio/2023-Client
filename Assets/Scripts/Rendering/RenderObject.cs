using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class RenderObject : MonoBehaviour
{
    public int meshGuid;

    private void OnEnable()
    {
        //GPUDrivenManager.Instance.AddRenderObject(meshGuid, transform);
    }

    private void OnDisable()
    {
        //GPUDrivenManager.Instance.RemoveRenderObject(meshGuid, transform);
    }
}
