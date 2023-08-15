using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GrassBendingObject : MonoBehaviour{
    [SerializeField] private UniversalRendererData rendererSettings = null;

    private bool TryGetFeature(out GrassBendingRenderFeature feature) {
        feature = rendererSettings.rendererFeatures.OfType<GrassBendingRenderFeature>().FirstOrDefault();
        return feature != null;
    }

    private void OnEnable() {
        if(TryGetFeature(out var feature)) {
            feature.AddTrackedTransform(transform);
        }
    }

    private void OnDisable() {
        if(TryGetFeature(out var feature)) {
            feature.RemoveTrackedTransform(transform);
        }
    }
}