using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class FreezingEffect : MonoBehaviour
{
    [Header("References")]
    public Material vignetteMaterial;
    public FadeScreen fadeScreen;

    [Header("Settings")]
    public EasingType freezeEasing = EasingType.Linear;
    public float freezeDuration = 60f;
    public float fadeToBlackDuration = 3f;


    private bool isFreezing = false;
    private float elapsedTime = 0f;
    private float radius = 1f;
    private Color bgColor = Color.black;

    private void Awake()
    {
        radius = vignetteMaterial.GetFloat("_Radius");
        bgColor = vignetteMaterial.GetColor("_BgColor");
    }

    private void OnEnable()
    {
        Color color = new Color(0, 0, 0, 0.0f);
        vignetteMaterial.SetFloat("_Radius", 1f);
        vignetteMaterial.SetColor("_BgColor", color);
    }

    private void OnDisable()
    {
        vignetteMaterial.SetFloat("_Radius", radius);
        vignetteMaterial.SetColor("_BgColor", bgColor);
    }

    public void StartFreezing()
    {
        if (isFreezing)
            return;
            
        isFreezing = true;
        elapsedTime = 0f; 
        PlayerEvents.OnFreezingStart?.Invoke(freezeDuration);
    }

    public void StopFreezing()
    {
        if (!isFreezing) 
            return;

        isFreezing = false;
        vignetteMaterial.SetFloat("_Radius", 0f);
        PlayerEvents.OnFreezingEnd?.Invoke();

        fadeScreen?.FadeToColor(fadeToBlackDuration, Color.black, () =>
        {
            vignetteMaterial.SetFloat("_Radius", 1f);
        });
    }

    private void Update()
    {
        if (!isFreezing) return;

        elapsedTime += Time.deltaTime;
        float rawProgress = Mathf.Clamp01(elapsedTime / freezeDuration);
        float easedProgress = Easing.Evaluate(freezeEasing, rawProgress);
        vignetteMaterial.SetFloat("_Radius", Mathf.Lerp(1f, 0f, easedProgress));

        float frostFadeThreshold = 0.75f;
        if (rawProgress >= frostFadeThreshold)
        {
            float frostFadeProgress = (rawProgress - frostFadeThreshold) / (1f - frostFadeThreshold);

            float startAlpha = 0f;
            float endAlpha = .75f;
            float targetAlpha = Mathf.Lerp(startAlpha, endAlpha, frostFadeProgress);
            Color newColor = new Color(bgColor.r, bgColor.g, bgColor.b, targetAlpha);
            vignetteMaterial.SetColor("_BgColor", newColor);
        }

        if (elapsedTime >= freezeDuration)
        {
            StopFreezing();
        }
    }

}