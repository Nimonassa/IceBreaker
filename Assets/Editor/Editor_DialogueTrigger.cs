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

        // Use the Property Iterator to avoid hardcoding field order
        SerializedProperty prop = serializedObject.GetIterator();
        if (prop.NextVisible(true))
        {
            do
            {
                // Skip internal script reference
                if (prop.name == "m_Script") continue;

                // Skip properties handled in the custom Node Selection section at the bottom
                if (prop.name == "startingNode" || prop.name == "displayOnlyRootNodes" || prop.name == "autoAddedCollider") continue;

                // Conditional Visibility Logic
                if (prop.name == "startKey" && trigger.triggerType != TriggerType.KeyPress) continue;
                if (prop.name == "startAction" && trigger.triggerType != TriggerType.InputAction) continue;
                if ((prop.name == "triggerLayerMask" || prop.name == "triggerTag") && 
                    trigger.triggerType != TriggerType.OnTriggerEnter && trigger.triggerType != TriggerType.OnTriggerExit) continue;

                // Custom Handling for TriggerType (Auto-Collider Logic)
                if (prop.name == "triggerType")
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(prop);
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        HandleAutoCollider(trigger, (TriggerType)prop.enumValueIndex);
                    }
                }
                // Custom Handling for triggerTag (Tag Dropdown)
                else if (prop.name == "triggerTag")
                {
                    DrawTagPopup(prop);
                }
                // Draw everything else normally
                else
                {
                    EditorGUILayout.PropertyField(prop, true);
                }

            } while (prop.NextVisible(false));
        }

        serializedObject.ApplyModifiedProperties();

        // DRAW NODE SELECTION SECTION
        if (trigger.dialogueSceneGraph != null && trigger.dialogueSceneGraph.graph != null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Node Selection", EditorStyles.boldLabel);
            
            SerializedProperty displayRootsProp = serializedObject.FindProperty("displayOnlyRootNodes");
            EditorGUILayout.PropertyField(displayRootsProp);
            serializedObject.ApplyModifiedProperties();

            DrawNodeSelectionPopup(trigger, displayRootsProp.boolValue);
        }
        else
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Assign a Dialogue Scene Graph above to select a starting node.", MessageType.Info);
        }
    }

    private void HandleAutoCollider(DialogueTrigger trigger, TriggerType newType)
    {
        if (newType == TriggerType.OnTriggerEnter || newType == TriggerType.OnTriggerExit)
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
}