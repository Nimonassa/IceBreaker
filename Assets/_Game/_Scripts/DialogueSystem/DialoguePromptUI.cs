using UnityEngine;
using System.Collections;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class DialoguePromptUI : MonoBehaviour
{
    [Header("Settings")]
    public float fadeDuration = 0.25f;

    [Header("Prompt Objects")]
    // MODIFIED: Changed from GameObject to the new SinglePromptUI component
    public SinglePromptUI infoPrompt;
    public SinglePromptUI successPrompt;
    public SinglePromptUI warningPrompt;
    public SinglePromptUI failurePrompt;

    public bool IsVisible { get; private set; }

    private CanvasGroup canvasGroup;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        HideAllPrompts();
        gameObject.SetActive(false);
    }
    
    private void HideAllPrompts()
    {
        // MODIFIED: Access the gameObject through the component to set it inactive
        if (infoPrompt != null) 
            infoPrompt.gameObject.SetActive(false);
        if (successPrompt != null) 
            successPrompt.gameObject.SetActive(false);
        if (warningPrompt != null) 
            warningPrompt.gameObject.SetActive(false);
        if (failurePrompt != null) 
            failurePrompt.gameObject.SetActive(false);
    }

    public void Show(string text, PromptType type)
    {
        IsVisible = true;
        gameObject.SetActive(true);

        HideAllPrompts();

        // MODIFIED: Type changed from GameObject to SinglePromptUI
        SinglePromptUI activePrompt = null;
        switch (type)
        {
            case PromptType.Info: activePrompt = infoPrompt; break;
            case PromptType.Success: activePrompt = successPrompt; break;
            case PromptType.Warning: activePrompt = warningPrompt; break;
            case PromptType.Failure: activePrompt = failurePrompt; break;
        }

        if (activePrompt != null)
        {
            activePrompt.gameObject.SetActive(true);
            activePrompt.SetText(text); 
        }

        if (fadeRoutine != null) 
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(1f));
    }

    public void Hide()
    {
        if (!IsVisible)
            return;
            
        IsVisible = false;

        if (fadeRoutine != null) 
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(0f, () => gameObject.SetActive(false)));
    }

    private IEnumerator FadeRoutine(float target, System.Action onComplete = null)
    {
        float start = canvasGroup.alpha;
        float time = 0;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, time / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = target;
        onComplete?.Invoke();
        fadeRoutine = null;
    }
}