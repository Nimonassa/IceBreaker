using UnityEngine;

public class CupLiquid : MonoBehaviour
{
    public Transform liquid;
    public float maxFillHeight = 0.1f;
    public float fillAmount = 0.8f; 

    void Update()
    {
        Vector3 scale = liquid.localScale;
        scale.y = Mathf.Lerp(0.01f, maxFillHeight, fillAmount);
        liquid.localScale = scale;

        Vector3 pos = liquid.localPosition;
        pos.y = scale.y / 2f;
        liquid.localPosition = pos;
    }
}