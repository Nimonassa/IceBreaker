using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueUI : MonoBehaviour
{
    [Header("Sub-Systems")]
    public DialogueChatUI chatUI;
    public DialogueChoiceUI choiceUI;
    public DialoguePromptUI promptUI;

    [Header("Master Settings")]
    public float fadeDuration = 0.25f;
    public GameObject UIContainer;
    private CanvasGroup uiCanvasGroup;
    private Coroutine masterFadeRoutine;

    private void Awake()
    {
        Debug.Assert(chatUI != null, "DialogueUI: chatUI is missing! Please assign it in the Inspector.");
        Debug.Assert(choiceUI != null, "DialogueUI: choiceUI is missing! Please assign it in the Inspector.");
        Debug.Assert(promptUI != null, "DialogueUI: promptUI is missing! Please assign it in the Inspector.");

        if (UIContainer == null) UIContainer = this.gameObject;

        uiCanvasGroup = UIContainer.GetComponent<CanvasGroup>();
        if (uiCanvasGroup == null) uiCanvasGroup = UIContainer.AddComponent<CanvasGroup>();

        uiCanvasGroup.alpha = 0;
        uiCanvasGroup.blocksRaycasts = false;
        uiCanvasGroup.interactable = false;
        UIContainer.SetActive(false);
    }

    public void SetVisible(bool visible)
{
    if (masterFadeRoutine != null) StopCoroutine(masterFadeRoutine);

    if (visible)
    {
        gameObject.SetActive(true);
        UIContainer.SetActive(true);
        masterFadeRoutine = StartCoroutine(MasterFade(true));
    }
    else
    {
        if (chatUI.IsVisible) chatUI.Hide();
        if (promptUI.IsVisible) promptUI.Hide();
        if (choiceUI.IsVisible) choiceUI.Hide();

        if (gameObject.activeInHierarchy)
        {
            masterFadeRoutine = StartCoroutine(MasterFade(false));
        }
        else
        {
            uiCanvasGroup.alpha = 0f;
            UIContainer.SetActive(false);
        
            chatUI.gameObject.SetActive(false);
            promptUI.gameObject.SetActive(false);
            choiceUI.gameObject.SetActive(false);
        }
    }
}

    public void ShowChat(string speaker, string text)
    {
        // 1. Ensure parent is awake
        gameObject.SetActive(true);
        UIContainer.SetActive(true);

        if (promptUI.gameObject.activeInHierarchy) promptUI.Hide();

        if (string.IsNullOrWhiteSpace(text))
        {
            if (chatUI.gameObject.activeInHierarchy) 
                chatUI.Hide();
        }
        else
        {
            chatUI.gameObject.SetActive(true);
            chatUI.Show(speaker, text);
        }
    }

    public void ShowPrompt(string text, PromptType type)
    {
        gameObject.SetActive(true);
        UIContainer.SetActive(true);

        if (chatUI.gameObject.activeInHierarchy) 
            chatUI.Hide();

        if (string.IsNullOrWhiteSpace(text))
        {
            if (promptUI.gameObject.activeInHierarchy) 
                promptUI.Hide();
        }
        else
        {
            promptUI.gameObject.SetActive(true);
            promptUI.Show(text, type);
        }
    }

    public void ShowChoices(List<DialogueChoice> choices, GameLanguage language, System.Action<int> onChoiceSelected)
    {
        gameObject.SetActive(true);
        UIContainer.SetActive(true);

        bool hasOtherContent = chatUI.IsVisible || promptUI.IsVisible;

        choiceUI.gameObject.SetActive(true);
        choiceUI.Show(choices, language, hasOtherContent, onChoiceSelected);
    }

    public void ClearChoices()
    {
        if (choiceUI.gameObject.activeInHierarchy)
        {
            choiceUI.Hide();
        }
    }

    private IEnumerator MasterFade(bool visible)
    {
        float start = uiCanvasGroup.alpha;
        float target = visible ? 1f : 0f;
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            uiCanvasGroup.alpha = Mathf.Lerp(start, target, time / fadeDuration);
            yield return null;
        }

        uiCanvasGroup.alpha = target;
        uiCanvasGroup.blocksRaycasts = visible;
        uiCanvasGroup.interactable = visible;

        if (!visible)
        {
            UIContainer.SetActive(false);
            chatUI.gameObject.SetActive(false);
            promptUI.gameObject.SetActive(false);
            choiceUI.gameObject.SetActive(false);
        }

        masterFadeRoutine = null;
    }

}
