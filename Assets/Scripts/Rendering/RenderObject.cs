using UnityEngine;

public class RenderObject : MonoBehaviour
{
    public string meshName;

    private void OnEnable()
    {
        GPUDrivenManager.Instance.AddRenderObject(meshName, transform);
    }

    private void OnDisable()
    {
        GPUDrivenManager.Instance.RemoveRenderObject(meshName, transform);
    }
}
