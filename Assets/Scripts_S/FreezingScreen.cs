using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FreezingScreen : MonoBehaviour
{
    public Stopwatch stopwatch;
    public Volume postProcessVolume;
    private Vignette vignette;

    private Coroutine transitionCoroutine;

    void Start()
    {
        stopwatch.events.onTick.AddListener(OnTick);
        postProcessVolume.profile.TryGet(out vignette);
    }

    public void OnTick()
    {
        if (vignette == null || !gameObject.activeInHierarchy)
            return;

        float targetProgress = stopwatch.GetProgress();

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(SmoothTransition(targetProgress));
    }

    private IEnumerator SmoothTransition(float targetIntensity)
    {
        float startIntensity = vignette.intensity.value;
        float elapsedTime = 0f;
        float duration = 1f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float t = elapsedTime / duration;

         
            float easedT = EaseOutSine(t);

            vignette.intensity.value =
                Mathf.Lerp(startIntensity, targetIntensity, easedT);

            yield return null;
        }

        vignette.intensity.value = targetIntensity;
    }

    // the correct mathematical function 
    private float EaseOutSine(float x)
    {
        return Mathf.Sin((x * Mathf.PI) / 2f);
    }
}