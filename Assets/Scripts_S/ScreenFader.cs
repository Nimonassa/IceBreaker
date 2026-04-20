using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeScreen : MonoBehaviour
{
    public Image fadeImage;

    public void FadeToColor(float duration, Color targetColor, System.Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(duration, targetColor, onComplete));
    }

    public void FadeToClear(float duration, System.Action onComplete = null)
    {
        Color clearColor = fadeImage.color;
        clearColor.a = 0;
        FadeToColor(duration, clearColor, onComplete);
    }

    private IEnumerator FadeRoutine(float duration, Color targetColor, System.Action onComplete)
    {
        Color startColor = fadeImage.color;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            fadeImage.color = Color.Lerp(startColor, targetColor, time / duration);
            yield return null;
        }

        fadeImage.color = targetColor;
        onComplete?.Invoke();
    }
}