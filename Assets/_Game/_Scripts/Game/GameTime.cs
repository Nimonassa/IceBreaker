using System.Collections;
using UnityEngine;

public class GameTime : MonoBehaviour
{
    public static GameTime Instance { get; private set; }

    private Coroutine timeTransitionCoroutine;

    private void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    public void SetTimeScale(float targetScale, float transitionDuration = 0f)
    {
        if (timeTransitionCoroutine != null) 
        {
            StopCoroutine(timeTransitionCoroutine);
        }

        if (transitionDuration <= 0f)
        {
            Time.timeScale = targetScale;
        }
        else
        {
            timeTransitionCoroutine = StartCoroutine(TransitionTimeScale(targetScale, transitionDuration));
        }
    }


    private IEnumerator TransitionTimeScale(float targetScale, float duration)
    {
        float startScale = Time.timeScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(startScale, targetScale, elapsed / duration);
            yield return null;
        }

        Time.timeScale = targetScale;
    }
}