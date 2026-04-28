using UnityEngine;
using UnityEngine.Events;
using XNode;
using System.Collections.Generic;

[CreateNodeMenu("Dialogue/Choice Node")]
public class ChoiceNode : BaseNode 
{

    [Header("NPC Prompt (Localized)")]
    [HideInInspector] public string[] localizedSpeakerNames = new string[1];
    [HideInInspector, TextArea(2, 3)] public string[] localizedPromptTexts = new string[1]; 
    [HideInInspector] public AudioClip[] localizedPromptAudios = new AudioClip[1];

    [Header("Player Choices")]
    [Output(dynamicPortList = true, backingValue = ShowBackingValue.Never)] 
    public List<DialogueChoice> choices = new List<DialogueChoice>();

    [HideInInspector] public string choicesFieldName = "choices"; 

    // --- NPC Prompt Helpers ---

    public string GetSpeakerName(GameLanguage lang)
    {
        int i = (int)lang;
        return (i >= 0 && i < localizedSpeakerNames.Length) ? localizedSpeakerNames[i] : "";
    }

    public string GetPromptText(GameLanguage lang)
    {
        int i = (int)lang;
        return (i >= 0 && i < localizedPromptTexts.Length) ? localizedPromptTexts[i] : "";
    }

    public AudioClip GetAudio(GameLanguage lang)
    {
        int i = (int)lang;
        return (i >= 0 && i < localizedPromptAudios.Length) ? localizedPromptAudios[i] : null;
    }

    public string GetChoiceText(int choiceIndex, GameLanguage lang)
    {
        if (choiceIndex >= 0 && choiceIndex < choices.Count)
        {
            return choices[choiceIndex].GetText(lang);
        }
        return "";
    }

    public BaseNode GetNextNode(int choiceIndex) 
    {
        NodePort outPort = GetOutputPort(choicesFieldName + " " + choiceIndex);
        if (outPort != null && outPort.IsConnected) 
        {
            return outPort.Connection.node as BaseNode;
        }
        return null;
    }
}

[System.Serializable]
public class DialogueChoice
{
    [TextArea(2, 4)]
    public string[] localizedChoiceTexts = new string[1];
    [xNodeUnityEvent] public UnityEvent onChoicePicked;

    public string GetText(GameLanguage lang)
    {
        int i = (int)lang;
        return (i >= 0 && i < localizedChoiceTexts.Length) ? localizedChoiceTexts[i] : "";
    }
}
