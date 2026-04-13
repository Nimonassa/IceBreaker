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
    private System.Action onCurrentDialogueComplete = null;
    private int lastDialogueEndFrame = -1;
    public bool IsDialogueActive => currentNode != null || Time.frameCount == lastDialogueEndFrame;
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

    public void StartDialogue(BaseNode startNode, System.Action onComplete = null)
    {
        onCurrentDialogueComplete = onComplete;

        if (startNode == null)
        {
            Debug.LogWarning("DialogueManager: StartDialogue called with null node.");
            EndDialogue();
            return;
        }
    
        dialogueUI.SetVisible(true);
        OnDialogueStarted?.Invoke();
        EnterNode(startNode);
    }

    private void EnterNode(BaseNode node)
    {
        if ((currentNode = node) == null)
        {
            EndDialogue();
            return;
        }

        if (node is ChatNode chat)
            HandleChat(chat);
        else if (node is ChoiceNode choice)
            HandleChoice(choice);
        else if (node is PromptNode prompt)
            HandlePrompt(prompt);
    }

    private void HandleChat(ChatNode node)
    {
        node.onNodeTriggered?.Invoke();

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

        // EXACT TIMING LOGIC
        float delay = 0;
        bool shouldStartTimer = false;

        switch (node.autoAdvanceMode)
        {
            case AutoAdvanceMode.Timer:
                delay = node.displayDuration;
                shouldStartTimer = true;
                break;
            case AutoAdvanceMode.Audio:
                // Use clip length if it exists, otherwise 1.5s fallback so it doesn't get stuck
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
        node.onNodeTriggered?.Invoke();

        if (IsNodeEmpty(node))
        {
            Debug.Log($"Skipping empty ChatNode: {node.name}");
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

        if (currentNode is ChatNode chat && chat.GetOutputPort("exit").IsConnected)
        {
            EnterNode(chat.GetOutputPort("exit").Connection.node as BaseNode);
        }
        else if (currentNode is PromptNode prompt && prompt.GetOutputPort("exit").IsConnected)
        {
            EnterNode(prompt.GetOutputPort("exit").Connection.node as BaseNode);
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

        node.onNodeTriggered?.Invoke();
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
            EnterNode(node.GetNextNode(index));
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
        currentNode = null;

        // MODIFIED: Record the frame we ended on before firing the event
        lastDialogueEndFrame = Time.frameCount;
        OnDialogueEnded?.Invoke();
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

        // It's empty if there is no text AND no audio
        return string.IsNullOrWhiteSpace(text) && audio == null;
    }

}