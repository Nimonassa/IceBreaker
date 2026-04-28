using UnityEngine;
using UnityEngine.Events;
using XNode;


[CreateNodeMenu("Dialogue/Chat Node")]
public class ChatNode : BaseNode
{
    // Translatable fields
    [HideInInspector] public string[] localizedSpeakerNames = new string[1];
    [HideInInspector, TextArea(3, 5)] public string[] localizedTexts = new string[1];
    [HideInInspector] public AudioClip[] localizedAudios = new AudioClip[1];

    [Header("Timing")]
    [xNodeEnum] public AutoAdvanceMode autoAdvanceMode = AutoAdvanceMode.Timer;
    public float displayDuration = 3f;

    [Output(ShowBackingValue.Never, ConnectionType.Override)]
    public int exit;

    public string GetSpeakerName(GameLanguage lang)
    {
        int i = (int)lang;
        return (i >= 0 && i < localizedSpeakerNames.Length) ? localizedSpeakerNames[i] : "";
    }

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
