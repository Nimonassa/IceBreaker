using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TriggerEvent))]
public class TriggerEventEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (prop.name == "m_Script")
            {
                continue;
            }

            bool isManual = serializedObject.FindProperty("mode").enumValueIndex == (int)TriggerEvent.TriggerMode.Manual;

            if (isManual && (prop.name == "useLayer" || prop.name == "targetLayer" || prop.name == "useTag" || prop.name == "targetTag"))
            {
                continue;
            }

            if (prop.name == "targetLayer" && !serializedObject.FindProperty("useLayer").boolValue)
            {
                continue;
            }

            if (prop.name == "targetTag" && !serializedObject.FindProperty("useTag").boolValue)
            {
                continue;
            }

            if (prop.name == "targetTag")
            {
                prop.stringValue = EditorGUILayout.TagField(new GUIContent("Target Tag"), prop.stringValue);
            }
            else
            {
                EditorGUILayout.PropertyField(prop, true);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}