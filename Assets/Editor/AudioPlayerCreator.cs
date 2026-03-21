using UnityEditor;
using UnityEngine;

public static class AudioPlayerCreator
{
    // The priority of 10 groups it nicely with other standard Unity creation items
    [MenuItem("GameObject/Audio/Audio Player", false, 10)]
    public static void CreateAudioPlayer(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("Audio Player");
        go.AddComponent<AudioPlayer>();
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        Selection.activeObject = go;
    }
}