using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
public class DialogueChoiceUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject choiceButtonPrefab;
    public Transform buttonContainer;

    [Header("Settings")]
    public float fadeDuration = 0.25f;
    public float moveDuration = 0.5f;
    public float transitionDuration = 0.2f; // Added for choice-to-choice fading

    public bool IsVisible { get; private set; }

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 storedPosition;
    private List<GameObject> pooledButtons = new List<GameObject>();

    private int activeButtonCount = 0; // Tracks how many buttons are currently showing

    private Coroutine fadeRoutine;
    private Coroutine moveRoutine;
    private Coroutine transitionRoutine; // Added to manage the transition state

    private void Awake()
    {
        if (buttonContainer == null) buttonContainer = this.transform;

        foreach (Transform child in buttonContainer) child.gameObject.SetActive(false);

        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null)
            storedPosition = rectTransform.anchoredPosition;

        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        gameObject.SetActive(false);
    }

    public void Show(List<DialogueChoice> choices, GameLanguage lang, bool hasOtherContent, System.Action<int> onChoiceSelected)
    {
        gameObject.SetActive(true);
        bool wasVisible = IsVisible;
        IsVisible = true;

        Vector2 targetPos = hasOtherContent ? storedPosition : Vector2.zero;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);

        if (rectTransform != null)
        {
            if (wasVisible) moveRoutine = StartCoroutine(MoveRoutine(targetPos));
            else rectTransform.anchoredPosition = targetPos;
        }

        // MODIFIED: Branch logic based on whether choices are already on screen
        if (wasVisible)
        {
            transitionRoutine = StartCoroutine(TransitionChoicesRoutine(choices, lang, onChoiceSelected));
        }
        else
        {
            SetupButtons(choices, lang, onChoiceSelected);
            fadeRoutine = StartCoroutine(FadeRoutine(1f));
        }
    }


    public void Hide()
    {
        if (!IsVisible) return;
        IsVisible = false;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        if (transitionRoutine != null) StopCoroutine(transitionRoutine); // Clean up transition

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        fadeRoutine = StartCoroutine(FadeRoutine(0f, () => gameObject.SetActive(false)));
    }

    // NEW: Handles seamless fading between different amounts of choices
    // NEW: Handles seamless fading between different amounts of choices
    private IEnumerator TransitionChoicesRoutine(List<DialogueChoice> choices, GameLanguage lang, System.Action<int> onChoiceSelected)
    {
        canvasGroup.interactable = false; // Block clicks during transition

        int previousCount = activeButtonCount;
        int newCount = choices.Count;
        int reuseCount = Mathf.Min(previousCount, newCount);

        // Ensure we have enough pooled buttons
        while (pooledButtons.Count < newCount)
        {
            pooledButtons.Add(Instantiate(choiceButtonPrefab, buttonContainer));
        }

        // --- PHASE 1: Instantly Setup All Needed Buttons ---
        for (int i = 0; i < pooledButtons.Count; i++)
        {
            GameObject btnObj = pooledButtons[i];

            if (i < newCount)
            {
                btnObj.SetActive(true);

                // Instantly update text and ensure text is fully visible
                var textComp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (textComp != null)
                {
                    textComp.text = choices[i].GetText(lang);
                    SetTextAlpha(btnObj, 1f);
                }

                // If it's a reused button, ensure its CanvasGroup is fully visible right away
                if (i < reuseCount)
                {
                    GetOrAddCanvasGroup(btnObj).alpha = 1f;
                }
                else
                {
                    // If it's a brand new button, start it at 0 alpha for the fade-in
                    GetOrAddCanvasGroup(btnObj).alpha = 0f;
                }

                // Re-bind the click events
                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    int index = i;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        if (!canvasGroup.interactable) return;
                        canvasGroup.interactable = false;
                        onChoiceSelected?.Invoke(index);
                    });
                }
            }
            else if (i >= previousCount)
            {
                // Ensure buttons beyond the previous count and new count are completely off
                btnObj.SetActive(false);
            }
        }

        // --- PHASE 2: Crossfade the Difference ONLY if counts changed ---
        if (previousCount != newCount)
        {
            float time = 0;
            while (time < transitionDuration)
            {
                time += Time.deltaTime;
                float progress = time / transitionDuration;

                // Fade OUT extra buttons we no longer need (if previousCount > newCount)
                for (int i = reuseCount; i < previousCount; i++)
                {
                    GetOrAddCanvasGroup(pooledButtons[i]).alpha = Mathf.Lerp(1f, 0f, progress);
                }

                // Fade IN new buttons we just added (if newCount > previousCount)
                for (int i = reuseCount; i < newCount; i++)
                {
                    GetOrAddCanvasGroup(pooledButtons[i]).alpha = Mathf.Lerp(0f, 1f, progress);
                }

                yield return null;
            }
        }

        // --- PHASE 3: Final Cleanup ---
        for (int i = 0; i < pooledButtons.Count; i++)
        {
            if (i < newCount)
            {
                GetOrAddCanvasGroup(pooledButtons[i]).alpha = 1f;
            }
            else
            {
                GetOrAddCanvasGroup(pooledButtons[i]).alpha = 0f;
                pooledButtons[i].SetActive(false); // Hide the unused ones completely
            }
        }

        activeButtonCount = newCount;
        canvasGroup.interactable = true;
        transitionRoutine = null;
    }
    
    
    

    private void SetupButtons(List<DialogueChoice> choices, GameLanguage lang, System.Action<int> onChoiceSelected)
    {
        activeButtonCount = choices.Count; // Track active buttons
        foreach (var btn in pooledButtons) btn.SetActive(false);

        for (int i = 0; i < choices.Count; i++)
        {
            if (i >= pooledButtons.Count)
            {
                pooledButtons.Add(Instantiate(choiceButtonPrefab, buttonContainer));
            }

            GameObject btnObj = pooledButtons[i];
            btnObj.SetActive(true);

            // Ensure states are fully reset in case a transition was interrupted
            GetOrAddCanvasGroup(btnObj).alpha = 1f;

            var textComp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComp != null)
            {
                textComp.text = choices[i].GetText(lang);
                SetTextAlpha(btnObj, 1f);
            }

            int index = i;
            Button btn = btnObj.GetComponent<Button>();

            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    if (!canvasGroup.interactable) return;
                    canvasGroup.interactable = false;
                    onChoiceSelected?.Invoke(index);
                });
            }
        }
    }


    // Helper: Safely adjusts the color alpha of the TextMeshPro component
    private void SetTextAlpha(GameObject btnObj, float alpha)
    {
        var textComp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textComp != null)
        {
            Color c = textComp.color;
            c.a = alpha;
            textComp.color = c;
        }
    }

    // Helper: Injects a CanvasGroup on instantiated buttons if they don't already have one
    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = obj.AddComponent<CanvasGroup>();
        return cg;
    }

    private IEnumerator MoveRoutine(Vector2 target)
    {
        Vector2 start = rectTransform.anchoredPosition;
        float time = 0;
        while (time < moveDuration)
        {
            time += Time.deltaTime;
            rectTransform.anchoredPosition = Vector2.Lerp(start, target, time / moveDuration);
            yield return null;
        }
        rectTransform.anchoredPosition = target;
        moveRoutine = null;
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

        bool isVisible = (target >= 1f);
        canvasGroup.blocksRaycasts = isVisible;
        canvasGroup.interactable = isVisible;

        onComplete?.Invoke();
        fadeRoutine = null;
    }
}

