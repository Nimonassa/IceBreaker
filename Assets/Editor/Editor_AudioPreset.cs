using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioPreset))]
public class AudioPresetEditor : Editor
{
    SerializedProperty clipMode;
    SerializedProperty singleClip;
    SerializedProperty multipleClips;
    SerializedProperty playMode;
    SerializedProperty volume;
    SerializedProperty pitch;
    SerializedProperty pitchRandomness;

    private void OnEnable()
    {
        clipMode = serializedObject.FindProperty("clipMode");
        singleClip = serializedObject.FindProperty("singleClip");
        multipleClips = serializedObject.FindProperty("multipleClips");
        playMode = serializedObject.FindProperty("playMode");
        volume = serializedObject.FindProperty("volume");
        pitch = serializedObject.FindProperty("pitch");
        pitchRandomness = serializedObject.FindProperty("pitchRandomness");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Always show the mode selector
        EditorGUILayout.PropertyField(clipMode);
        EditorGUILayout.Space();

        // Conditionally show fields based on the selected enum
        if (clipMode.enumValueIndex == (int)ClipMode.Single)
        {
            EditorGUILayout.PropertyField(singleClip);
        }
        else
        {
            EditorGUILayout.PropertyField(multipleClips, true);
            EditorGUILayout.PropertyField(playMode);
        }

        EditorGUILayout.Space();

        // REMOVED the manual LabelField here! 
        // Unity will automatically draw the [Header("Settings")] from the volume property.

        EditorGUILayout.PropertyField(volume);
        EditorGUILayout.PropertyField(pitch);
        EditorGUILayout.PropertyField(pitchRandomness);

        serializedObject.ApplyModifiedProperties();
    }

}