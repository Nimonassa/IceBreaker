using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(InteractionTrigger))]
public class InteractionTriggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        InteractionTrigger trigger = (InteractionTrigger)target;
        serializedObject.Update();

        // --- Basic Setup ---
        EditorGUILayout.PropertyField(serializedObject.FindProperty("triggerOnce"));
        EditorGUILayout.Space(10);

        // --- HEADER 1: TRIGGER MODE OPTIONS ---
        SerializedProperty triggerModeProp = serializedObject.FindProperty("triggerMode");
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(triggerModeProp);
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            HandleAutoCollider(trigger, (TriggerMode)triggerModeProp.enumValueIndex);
        }

        if (trigger.triggerMode != TriggerMode.None)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("triggerLayerMask"));
            DrawTagPopup(serializedObject.FindProperty("triggerTag"));
        }
        EditorGUILayout.Space(10);

        // --- HEADER 2: INPUT OPTIONS ---
        EditorGUILayout.LabelField("Input Options", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("inputType"));

        if (trigger.inputType != InputType.None)
        {
            if (trigger.inputType == InputType.Keyboard)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("startKey"));
                
            else if (trigger.inputType == InputType.InputAction)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("startAction"));
                
            else if (trigger.inputType == InputType.XRHand)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("xrTarget"), new GUIContent("XR Target Object"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("xrButton"));
            }
        }

        // --- Events ---
        EditorGUILayout.Space(10);
        DrawLine();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onTriggered"));

        serializedObject.ApplyModifiedProperties();
    }

    private void HandleAutoCollider(InteractionTrigger trigger, TriggerMode newMode)
    {
        if (newMode != TriggerMode.None)
        {
            if (trigger.GetComponent<Collider>() == null)
            {
                Undo.RecordObject(trigger, "Added Auto Collider");
                BoxCollider bc = Undo.AddComponent<BoxCollider>(trigger.gameObject);
                bc.isTrigger = true;
                trigger.autoAddedCollider = true;
            }
        }
        else if (trigger.autoAddedCollider)
        {
            BoxCollider bc = trigger.GetComponent<BoxCollider>();
            if (bc != null) Undo.DestroyObjectImmediate(bc);
            Undo.RecordObject(trigger, "Removed Auto Collider");
            trigger.autoAddedCollider = false;
        }
    }

    private void DrawTagPopup(SerializedProperty tagProp)
    {
        List<string> tagOptions = new List<string> { "Nothing" };
        tagOptions.AddRange(UnityEditorInternal.InternalEditorUtility.tags);
        
        int selectedIndex = tagOptions.IndexOf(tagProp.stringValue);
        if (selectedIndex == -1) selectedIndex = 0;
        
        selectedIndex = EditorGUILayout.Popup("Trigger Tag", selectedIndex, tagOptions.ToArray());
        tagProp.stringValue = tagOptions[selectedIndex];
    }

    private void DrawLine()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1));
        EditorGUILayout.Space(5);
    }
}