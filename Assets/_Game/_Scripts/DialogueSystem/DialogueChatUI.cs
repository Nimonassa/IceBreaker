using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class DialogueChatUI : MonoBehaviour
{
    [Header("Fading Settings")]
    public float fadeDuration = 0.25f;

    [Header("Typewriter Settings")]
    public bool useTypewriter = true;
    public float typingDelay = 0.02f;

    [Header("General UI")]
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;

    public bool IsVisible { get; private set; }

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Coroutine fadeRoutine;
    private Coroutine typeRoutine;

    // Specific RectTransform caches for the speaker chain
    private RectTransform speakerRT;
    private RectTransform speakerParent1;
    private RectTransform speakerParent2;
    private RectTransform dialogueRT;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        CacheLayoutReferences();

        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    private void CacheLayoutReferences()
    {
        if (speakerText != null)
        {
            speakerRT = speakerText.rectTransform; // The text's own RectTransform

            // Cache the two parents above the speaker text
            Transform p1 = speakerText.transform.parent;
            if (p1 != null && p1 is RectTransform rt1) speakerParent1 = rt1;

            if (p1 != null && p1.parent != null && p1.parent is RectTransform rt2)
                speakerParent2 = rt2;
        }

        if (dialogueText != null)
        {
            dialogueRT = dialogueText.rectTransform;
        }
    }

    public void Show(string speaker, string text)
    {
        IsVisible = true;
        gameObject.SetActive(true);

        speakerText.text = speaker;
        dialogueText.text = text;
        dialogueText.maxVisibleCharacters = useTypewriter ? 0 : 99999;

        // Force the layout to snap to the new name and text immediately
        ForceLayoutUpdate();

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        if (typeRoutine != null) StopCoroutine(typeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(1f));

        if (useTypewriter)
        {
            typeRoutine = StartCoroutine(TypeTextRoutine());
        }
    }

    public void Hide()
    {
        if (!IsVisible) return;
        IsVisible = false;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        if (typeRoutine != null) StopCoroutine(typeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(0f, () => gameObject.SetActive(false)));
    }

    /// <summary>
    /// Forces a strict bottom-up rebuild starting from the text components themselves.
    /// </summary>
    private void ForceLayoutUpdate()
    {
        // 1. Tell TMP to calculate its internal mesh bounds for the new strings
        speakerText.ForceMeshUpdate();
        dialogueText.ForceMeshUpdate();

        // 2. Inform the global Canvas system that data has changed
        Canvas.ForceUpdateCanvases();

        // 3. REBUILD SPEAKER CHAIN (Innermost to Outermost)
        if (speakerRT != null) LayoutRebuilder.ForceRebuildLayoutImmediate(speakerRT);
        if (speakerParent1 != null) LayoutRebuilder.ForceRebuildLayoutImmediate(speakerParent1);
        if (speakerParent2 != null) LayoutRebuilder.ForceRebuildLayoutImmediate(speakerParent2);

        // 4. REBUILD DIALOGUE CONTENT
        if (dialogueRT != null) LayoutRebuilder.ForceRebuildLayoutImmediate(dialogueRT);

        // 5. REBUILD ROOT (The whole bubble/container)
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
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

    private IEnumerator TypeTextRoutine()
    {
        int total = dialogueText.textInfo.characterCount;

        for (int i = 0; i <= total; i++)
        {
            dialogueText.maxVisibleCharacters = i;
            ForceLayoutUpdate();

            yield return new WaitForSeconds(typingDelay);
        }
        typeRoutine = null;
    }
}
