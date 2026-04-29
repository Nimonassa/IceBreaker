using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;
using XNode;

public enum AdvanceInputType
{
    Disabled,
    KeyPress,
    InputAction
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; } = null;

    [Header("Base Settings")]
    public GameLanguage currentLanguage;
    public DialogueUI dialogueUI;
    public DialogueAudio dialogueAudio;

    [Header("Advance Input Settings")]
    public AdvanceInputType advanceInputType = AdvanceInputType.KeyPress;
    public Key advanceKey = Key.Space;
    public InputActionReference advanceAction;

    public event Action OnDialogueStarted;
    public event Action OnDialogueAdvanced;
    public event Action OnDialogueEnded;

    private BaseNode currentNode;
    private Coroutine autoAdvanceCoroutine;
    private int lastDialogueEndFrame = -1;
    public bool IsDialogueActive => currentNode != null || Time.frameCount == lastDialogueEndFrame;

    // --- NEW ARCHITECTURE: The "Chamber" ---
    private BaseNode currentlyLoadedNode = null;
    private System.Action currentlyLoadedStartedCallback = null;
    private System.Action currentlyLoadedCompletedCallback = null;
    private System.Action onCurrentDialogueComplete = null;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        if (advanceAction != null)
        {
            advanceAction.action.performed += OnAdvancePerformed;
        }
    }

    private void OnDisable()
    {
        if (advanceAction != null)
        {
            advanceAction.action.performed -= OnAdvancePerformed;
        }
    }

    private void OnAdvancePerformed(InputAction.CallbackContext context)
    {
        if (advanceInputType == AdvanceInputType.InputAction)
        {
            TryManualAdvance();
        }
    }

    private void Update()
    {
        if (advanceInputType == AdvanceInputType.KeyPress && Keyboard.current != null)
        {
            if (Keyboard.current[advanceKey].wasPressedThisFrame)
            {
                TryManualAdvance();
            }
        }
    }

    private void TryManualAdvance()
    {
        if (currentNode == null) return;
        if (currentNode is ChoiceNode) return;

        AdvanceFromChat();
    }

    // --- LOAD AND PLAY LOGIC ---

    public void LoadDialogue(BaseNode node, System.Action onStarted = null, System.Action onCompleted = null)
    {
        currentlyLoadedNode = node;
        currentlyLoadedStartedCallback = onStarted;
        currentlyLoadedCompletedCallback = onCompleted;
    }

    public void PlayDialogue()
    {
        if (currentlyLoadedNode == null)
        {
            Debug.LogWarning("DialogueManager: PlayDialogue called but no dialogue is loaded.");
            return;
        }

        // 1. Transfer the completed callback to the active execution state
        onCurrentDialogueComplete = currentlyLoadedCompletedCallback;

        // 2. Cache the started callback locally for this specific execution
        System.Action startedCallback = currentlyLoadedStartedCallback;

        dialogueUI.SetVisible(true);

        // Fire global event for UI/Audio listeners
        OnDialogueStarted?.Invoke();

        // Fire the specific callback passed into LoadDialogue
        startedCallback?.Invoke();

        EnterNode(currentlyLoadedNode);
    }

    private void EnterNode(BaseNode node)
    {
        if ((currentNode = node) == null)
        {
            EndDialogue();
            return;
        }

        currentNode.onEnter?.Invoke();

        if (node is ChatNode chat)
            HandleChat(chat);
        else if (node is ChoiceNode choice)
            HandleChoice(choice);
        else if (node is PromptNode prompt)
            HandlePrompt(prompt);
    }

    private void HandleChat(ChatNode node)
    {
        if (IsNodeEmpty(node))
        {
            Debug.Log($"Skipping empty ChatNode: {node.name}");
            AdvanceFromChat();
            return;
        }

        dialogueUI.ShowChat(node.GetSpeakerName(currentLanguage), node.GetText(currentLanguage));
        dialogueUI.ClearChoices();

        AudioClip clip = node.GetAudio(currentLanguage);
        if (clip != null)
        {
            dialogueAudio.PlayVoice(clip);
        }
        else
        {
            dialogueAudio.StopVoice();
        }

        if (autoAdvanceCoroutine != null) StopCoroutine(autoAdvanceCoroutine);

        float delay = 0;
        bool shouldStartTimer = false;

        switch (node.autoAdvanceMode)
        {
            case AutoAdvanceMode.Timer:
                delay = node.displayDuration;
                shouldStartTimer = true;
                break;
            case AutoAdvanceMode.Audio:
                delay = (clip != null) ? clip.length : 1.5f;
                shouldStartTimer = true;
                break;
        }

        if (shouldStartTimer)
        {
            autoAdvanceCoroutine = StartCoroutine(AutoAdvance(delay));
        }
    }

    private void HandlePrompt(PromptNode node)
    {
        if (IsNodeEmpty(node))
        {
            Debug.Log($"Skipping empty PromptNode: {node.name}");
            AdvanceFromChat();
            return;
        }

        dialogueUI.ShowPrompt(node.GetText(currentLanguage), node.promptType);
        dialogueUI.ClearChoices();

        AudioClip clip = node.GetAudio(currentLanguage);
        if (clip != null)
        {
            dialogueAudio.PlayVoice(clip);
        }
        else
        {
            dialogueAudio.StopVoice();
        }

        if (autoAdvanceCoroutine != null)
            StopCoroutine(autoAdvanceCoroutine);

        float delay = 0;
        bool shouldStartTimer = false;

        switch (node.autoAdvanceMode)
        {
            case AutoAdvanceMode.Timer:
                delay = node.displayDuration;
                shouldStartTimer = true;
                break;
            case AutoAdvanceMode.Audio:
                delay = (clip != null) ? clip.length : 1.5f;
                shouldStartTimer = true;
                break;
        }

        if (shouldStartTimer)
        {
            autoAdvanceCoroutine = StartCoroutine(AutoAdvance(delay));
        }
    }

    private IEnumerator AutoAdvance(float delay)
    {
        yield return new WaitForSeconds(delay);
        AdvanceFromChat();
    }

    public void AdvanceFromChat()
    {
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }

        OnDialogueAdvanced?.Invoke();

        BaseNode nextNode = null;

        if (currentNode is ChatNode chat && chat.GetOutputPort("exit").IsConnected)
        {
            nextNode = chat.GetOutputPort("exit").Connection.node as BaseNode;
        }
        else if (currentNode is PromptNode prompt && prompt.GetOutputPort("exit").IsConnected)
        {
            nextNode = prompt.GetOutputPort("exit").Connection.node as BaseNode;
        }

        if (nextNode != null)
        {
            currentNode?.onExit?.Invoke();
            EnterNode(nextNode);
        }
        else
        {
            EndDialogue();
        }
    }

    private void HandleChoice(ChoiceNode node)
    {
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }

        dialogueUI.ShowChat(node.GetSpeakerName(currentLanguage), node.GetPromptText(currentLanguage));
        AudioClip clip = node.GetAudio(currentLanguage);
        if (clip != null)
        {
            dialogueAudio.PlayVoice(clip);
        }
        else
        {
            dialogueAudio.StopVoice();
        }

        dialogueUI.ShowChoices(node.choices, currentLanguage, (index) =>
        {
            OnDialogueAdvanced?.Invoke();

            node.choices[index].onChoicePicked?.Invoke();

            BaseNode nextNode = node.GetNextNode(index);
            if (nextNode != null)
            {
                node.onExit?.Invoke();
                EnterNode(nextNode);
            }
            else
            {
                EndDialogue();
            }
        });
    }

    public void EndDialogue()
    {
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }

        dialogueUI.SetVisible(false);
        dialogueUI.ClearChoices();
        dialogueAudio.StopVoice();

        currentNode?.onExit?.Invoke();
        currentNode = null;

        lastDialogueEndFrame = Time.frameCount;

        // Fire global event
        OnDialogueEnded?.Invoke();

        // Fire the specific callback passed into LoadDialogue
        onCurrentDialogueComplete?.Invoke();
        onCurrentDialogueComplete = null;
    }

    private bool IsNodeEmpty(BaseNode node)
    {
        string text = "";
        if (node is ChatNode chat) text = chat.GetText(currentLanguage);
        else if (node is PromptNode prompt) text = prompt.GetText(currentLanguage);

        AudioClip audio = null;
        if (node is ChatNode chatNode) audio = chatNode.GetAudio(currentLanguage);
        else if (node is PromptNode promptNode) audio = promptNode.GetAudio(currentLanguage);

        return string.IsNullOrWhiteSpace(text) && audio == null;
    }
}
