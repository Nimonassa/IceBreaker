using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FreezingScreen : MonoBehaviour
{
    public Stopwatch stopwatch;
    public Volume postProcessVolume;
    private Vignette vignette;

    // Tracks the active animation so we don't overlap them
    private Coroutine transitionCoroutine;

    void Start()
    {
        // Assuming your UnityEvent is directly on the stopwatch.
        // Change to stopwatch.events.onTick if you nested it in a struct.
        stopwatch.events.onTick.AddListener(OnTick);
        postProcessVolume.profile.TryGet(out vignette);
    }

    public void OnTick()
    {
        if (vignette == null || !gameObject.activeInHierarchy)
        {
            return;
        }

        float targetProgress = stopwatch.GetProgress();

        // Stop the previous second's animation if it is still running
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        // Start smoothly animating to the new progress value
        transitionCoroutine = StartCoroutine(SmoothTransition(targetProgress));
    }

    private IEnumerator SmoothTransition(float targetIntensity)
    {
        float startIntensity = vignette.intensity.value;
        float elapsedTime = 0f;
        float duration = 1f; // Ticks happen once per second, so we animate over 1 second

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            // Lerp smoothly blends from the old value to the new value
            vignette.intensity.value = Mathf.Lerp(startIntensity, targetIntensity, elapsedTime / duration);

            yield return null; // Wait for the next frame
        }

        // Ensure it hits the exact target value at the end
        vignette.intensity.value = targetIntensity;
    }
}
