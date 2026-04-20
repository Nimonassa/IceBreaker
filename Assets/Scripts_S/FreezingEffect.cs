using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class FreezingEffect : MonoBehaviour
{
    [Header("References")]
    public Material vignetteMaterial;
    public ScreenFader screenFader;

    [Header("Settings")]
    public EasingType freezeEasing = EasingType.Linear;
    public float freezeDuration = 60f;
    public float fadeToBlackDuration = 3f;


    private bool isFreezing = false;
    private float elapsedTime = 0f;


    private void OnDisable()
    {
        vignetteMaterial.SetFloat("_Radius", 1f);
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

        screenFader?.FadeToColor(fadeToBlackDuration, Color.black, () =>
        {
            vignetteMaterial.SetFloat("_Radius", 1f);
        });
    }

    private void Update()
    {
        if (!isFreezing) return;

        elapsedTime += Time.deltaTime;

        float progress = Mathf.Clamp01(elapsedTime / freezeDuration);
        float easedProgress = Easing.Evaluate(freezeEasing, progress);
        float currentRadius = Mathf.Lerp(1f, 0f, easedProgress);
        vignetteMaterial.SetFloat("_Radius", currentRadius);

        if (elapsedTime >= freezeDuration)
        {
            StopFreezing();
        }
    }
}