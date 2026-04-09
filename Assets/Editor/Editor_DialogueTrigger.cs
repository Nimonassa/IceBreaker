using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using XNode;


[CustomEditor(typeof(DialogueTrigger))]
public class DialogueTriggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DialogueTrigger trigger = (DialogueTrigger)target;
        serializedObject.Update();

        // --- Basic Setup ---
        EditorGUILayout.PropertyField(serializedObject.FindProperty("dialogueManager"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("dialogueSceneGraph"));
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
                // Shows the target slot and the button dropdown
                EditorGUILayout.PropertyField(serializedObject.FindProperty("xrTarget"), new GUIContent("XR Target Object"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("xrButton"));
            }
        }

        serializedObject.ApplyModifiedProperties();

        // --- Node Selection ---
        EditorGUILayout.Space(10);
        DrawLine();
        
        if (trigger.dialogueSceneGraph != null && trigger.dialogueSceneGraph.graph != null)
        {
            SerializedProperty displayRootsProp = serializedObject.FindProperty("displayOnlyRootNodes");
            EditorGUILayout.PropertyField(displayRootsProp);
            serializedObject.ApplyModifiedProperties();

            DrawNodeSelectionPopup(trigger, displayRootsProp.boolValue);
        }
        else
        {
            EditorGUILayout.HelpBox("Assign a Dialogue Scene Graph above to select a starting node.", MessageType.Info);
        }
    }

    private void HandleAutoCollider(DialogueTrigger trigger, TriggerMode newMode)
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

    private void DrawNodeSelectionPopup(DialogueTrigger trigger, bool onlyRoots)
    {
        NodeGraph graph = trigger.dialogueSceneGraph.graph;
        List<BaseNode> validNodes = new List<BaseNode>();
        List<string> popupOptions = new List<string>();

        foreach (Node node in graph.nodes)
        {
            if (node is BaseNode baseNode)
            {
                if (onlyRoots)
                {
                    NodePort enterPort = baseNode.GetInputPort("enter");
                    if (enterPort != null && !enterPort.IsConnected)
                    {
                        validNodes.Add(baseNode);
                        popupOptions.Add(baseNode.name);
                    }
                }
                else
                {
                    validNodes.Add(baseNode);
                    popupOptions.Add(baseNode.name);
                }
            }
        }

        if (validNodes.Count > 0)
        {
            int currentIndex = validNodes.IndexOf(trigger.startingNode);
            if (currentIndex < 0) 
            {
                currentIndex = 0;
                Undo.RecordObject(trigger, "Reset Starting Node to Valid Entry");
                trigger.startingNode = validNodes[0];
                EditorUtility.SetDirty(trigger);
            }

            int newIndex = EditorGUILayout.Popup("Start From Node:", currentIndex, popupOptions.ToArray());

            if (newIndex != currentIndex || trigger.startingNode == null)
            {
                Undo.RecordObject(trigger, "Changed Starting Node");
                trigger.startingNode = validNodes[newIndex];
                EditorUtility.SetDirty(trigger);
            }
        }
        else
        {
            EditorGUILayout.HelpBox(onlyRoots ? "No Root Nodes found!" : "No Dialogue Nodes found!", MessageType.Warning);
        }
    }

    private void DrawLine()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1));
        EditorGUILayout.Space(5);
    }
}