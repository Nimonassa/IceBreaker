using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using XNode;

[CustomEditor(typeof(DialogueTester))]
public class DialogueTesterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DialogueTester tester = (DialogueTester)target;

        // Draw the default Inspector for everything EXCEPT the hidden startingNode
        DrawDefaultInspector();

        // If we have a graph assigned, build the dropdown menu
        if (tester.dialogueSceneGraph != null && tester.dialogueSceneGraph.graph != null)
        {
            NodeGraph graph = tester.dialogueSceneGraph.graph;
            List<BaseNode> validNodes = new List<BaseNode>();
            List<string> popupOptions = new List<string>();

            // Find all valid Dialogue Nodes
            foreach (Node node in graph.nodes)
            {
                if (node is BaseNode baseNode)
                {
                    validNodes.Add(baseNode);

                    // --- FIX: Just use the node's name for the dropdown ---
                    string label = baseNode.name;

                    popupOptions.Add(label);
                }
            }

            // Draw the dropdown menu if we found nodes
            if (validNodes.Count > 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Testing Options", EditorStyles.boldLabel);

                // Find the index of the currently selected node
                int currentIndex = validNodes.IndexOf(tester.startingNode);
                if (currentIndex < 0) currentIndex = 0; // Default to the first one if null

                // Draw the actual popup menu
                int newIndex = EditorGUILayout.Popup("Start From Node:", currentIndex, popupOptions.ToArray());

                // Save the choice if it changed
                if (newIndex != currentIndex || tester.startingNode == null)
                {
                    Undo.RecordObject(tester, "Changed Starting Node");
                    tester.startingNode = validNodes[newIndex];
                    EditorUtility.SetDirty(tester); // Tells Unity to save the scene
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Your Dialogue Graph has no valid nodes yet!", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Assign a Dialogue Scene Graph above to select a starting node.", MessageType.Info);
        }
    }
}
