using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using XNode;

[CustomPropertyDrawer(typeof(DialogueReference))]
public class DialogueReferenceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Find the serialized properties
        SerializedProperty sceneGraphProp = property.FindPropertyRelative("sceneGraph");
        SerializedProperty nodeProp = property.FindPropertyRelative("node");

        // Layout constants
        float labelWidth = EditorGUIUtility.labelWidth;
        Rect labelRect = new Rect(position.x, position.y, labelWidth, position.height);
        Rect fieldRect = new Rect(position.x + labelWidth, position.y, (position.width - labelWidth) * 0.4f, position.height);
        Rect popupRect = new Rect(fieldRect.xMax + 5, position.y, (position.width - labelWidth) * 0.6f - 5, position.height);

        // 1. Draw the Label
        EditorGUI.LabelField(labelRect, label);

        // 2. Draw the DialogueSceneGraph slot
        EditorGUI.BeginChangeCheck();
        EditorGUI.PropertyField(fieldRect, sceneGraphProp, GUIContent.none);
        if (EditorGUI.EndChangeCheck())
        {
            nodeProp.objectReferenceValue = null;
        }

        // 3. Draw the Node Dropdown
        DialogueSceneGraph sceneGraph = sceneGraphProp.objectReferenceValue as DialogueSceneGraph;

        if (sceneGraph != null && sceneGraph.graph != null)
        {
            List<BaseNode> nodes = sceneGraph.graph.nodes.OfType<BaseNode>().ToList();

            if (nodes.Count > 0)
            {
                // Generate names and identify roots
                string[] nodeNames = nodes.Select(n =>
                {
                    string baseName = string.IsNullOrEmpty(n.name) ? n.GetType().Name : n.name;

                    // Logic: A root node has no input ports that are connected to anything
                    bool isRoot = !n.Inputs.Any(p => p.IsConnected);

                    return isRoot ? $"{baseName} (Root)" : baseName;
                }).ToArray();

                // Track current selection
                int currentIndex = nodes.IndexOf(nodeProp.objectReferenceValue as BaseNode);
                if (currentIndex == -1) currentIndex = 0;

                // Show the Popup
                int newIndex = EditorGUI.Popup(popupRect, currentIndex, nodeNames);
                nodeProp.objectReferenceValue = nodes[newIndex];
            }
            else
            {
                EditorGUI.LabelField(popupRect, "Graph has no BaseNodes", EditorStyles.miniLabel);
            }
        }
        else
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.LabelField(popupRect, "Assign a SceneGraph", EditorStyles.miniLabel);
            EditorGUI.EndDisabledGroup();
        }

        EditorGUI.EndProperty();
    }
}
