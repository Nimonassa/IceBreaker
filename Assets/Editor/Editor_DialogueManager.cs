using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DialogueManager))]
public class DialogueManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1. Draw the standard references
        EditorGUILayout.PropertyField(serializedObject.FindProperty("currentLanguage"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("dialogueUI"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("dialogueAudio"));


        // 2. Draw the Enum
        SerializedProperty advanceInputProp = serializedObject.FindProperty("advanceInputType");
        EditorGUILayout.PropertyField(advanceInputProp);

        // 3. Conditionally draw the corresponding field
        AdvanceInputType currentType = (AdvanceInputType)advanceInputProp.enumValueIndex;

        if (currentType == AdvanceInputType.KeyPress)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("advanceKey"));
        }
        else if (currentType == AdvanceInputType.InputAction)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("advanceAction"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
