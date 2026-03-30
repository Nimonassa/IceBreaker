using UnityEngine;
using UnityEngine.Events;
using XNode;

public enum PromptType
{
    Info,
    Success,
    Warning,
    Failure
}

[CreateNodeMenu("Dialogue/Prompt Node")]
public class PromptNode : BaseNode
{
    [Header("Prompt Settings")]
    [xNodeEnum] public PromptType promptType = PromptType.Info;

    [HideInInspector, TextArea(3, 5)] public string[] localizedTexts = new string[1];
    [HideInInspector] public AudioClip[] localizedAudios = new AudioClip[1];

    [Header("Timing")]
    [xNodeEnum] public AutoAdvanceMode autoAdvanceMode = AutoAdvanceMode.Timer;
    public float displayDuration = 3f;


    [Header("Game Logic")]
    [xNodeUnityEvent] public UnityEvent onNodeTriggered;

    [Output(ShowBackingValue.Never, ConnectionType.Override)]
    public int exit;

    public string GetText(GameLanguage lang)
    {
        int i = (int)lang;
        return (i >= 0 && i < localizedTexts.Length) ? localizedTexts[i] : "";
    }

    public AudioClip GetAudio(GameLanguage lang)
    {
        int i = (int)lang;
        return (i >= 0 && i < localizedAudios.Length) ? localizedAudios[i] : null;
    }
}