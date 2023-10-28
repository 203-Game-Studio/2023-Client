using UnityEngine;

public class RenderObject : MonoBehaviour
{
    public string meshName;

    private void OnEnable()
    {
        GPUDrivenManager.AddRenderObject(meshName, transform);
    }

    private void OnDisable()
    {
        GPUDrivenManager.RemoveRenderObject(meshName, transform);
    }
}