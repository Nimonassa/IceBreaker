using UnityEngine;
using UnityEditor;

public static class AudioTriggerCreator
{
    // This adds a new option to the GameObject right-click menu
    [MenuItem("GameObject/Audio/Audio Trigger", false, 10)]
    public static void CreateAudioTrigger(MenuCommand menuCommand)
    {
        GameObject triggerObj = new GameObject("Audio Trigger");
        
        Texture2D icon = EditorGUIUtility.IconContent("sv_label_4").image as Texture2D;
        EditorGUIUtility.SetIconForObject(triggerObj, icon);

        AudioTrigger trigger = triggerObj.AddComponent<AudioTrigger>();
        
        BoxCollider boxCollider = triggerObj.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        trigger.playOn = AudioTrigger.TriggerType.OnTriggerEnter;

        GameObject audioPlayer = new GameObject("Audio Player");
        audioPlayer.transform.SetParent(triggerObj.transform);
        audioPlayer.transform.localPosition = Vector3.zero;

        AudioPlayer player = audioPlayer.AddComponent<AudioPlayer>();
        player.maxDistance = 4;
        trigger.audioPlayer = player;

        GameObjectUtility.SetParentAndAlign(triggerObj, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(triggerObj, "Create " + triggerObj.name);
        Selection.activeObject = triggerObj;
    }
}