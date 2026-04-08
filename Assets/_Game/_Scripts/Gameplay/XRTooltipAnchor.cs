using UnityEngine;

public class XRTooltipAnchor : MonoBehaviour
{
    [SerializeField] private float radius = 0.005f;
    [SerializeField] private MeshRenderer meshRenderer;

    public Material Material => meshRenderer.sharedMaterial;

    public void SetRadius(float newRadius)
    {
        radius = newRadius;
        transform.localScale = Vector3.one * (radius * 2f); // Diameter
    }

    private void OnValidate()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        transform.localScale = Vector3.one * (radius * 2f);
    }
}