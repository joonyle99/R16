using UnityEngine;
using System.Collections.Generic;

public class ParallaxBackground : MonoBehaviour
{
    private List<ParallaxLayer> _parallaxLayers = new();

    public void Tick(float deltaTime)
    {
        foreach (var layer in _parallaxLayers)
        {
            layer.Tick();
        }
    }

    public void Initialize(Transform cameraTrans, Transform targetTrans)
    {
        if (cameraTrans == null) return;
        if (targetTrans == null) return;

        GetComponentsInChildren(includeInactive: true, _parallaxLayers);

        foreach (var layer in _parallaxLayers)
        {
            layer.Initialize(cameraTrans);
            layer.Activate();
        }
    }

    private void ActivateAll()
    {
        foreach (var layer in _parallaxLayers)
        {
            layer.Activate();
        }
    }
    private void DeactivateAll()
    {
        foreach (var layer in _parallaxLayers)
        {
            layer.DeActivate();
        }
    }
}
