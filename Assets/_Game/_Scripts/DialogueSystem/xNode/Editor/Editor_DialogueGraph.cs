using UnityEditor;
using UnityEngine;
using XNodeEditor;

[CustomNodeGraphEditor(typeof(DialogueGraph))]
public class DialogueGraphEditor : NodeGraphEditor
{
    public GameLanguage currentLanguage = GameLanguage.Finnish;
    public GameLanguage referenceLanguage = GameLanguage.Finnish;
    public bool showReferenceView = false;

    public override void OnOpen()
    {
        SyncLanguageToAllNodes();
    }

    public override void OnGUI()
    {
        base.OnGUI();

        // Increased height to 160 to comfortably fit the grouped boxes
        GUILayout.BeginArea(new Rect(10, 10, 320, 160), "Localization Settings", GUI.skin.window);

        
        EditorGUI.BeginChangeCheck();

        // --- GROUP 1: ACTIVE TARGET ---
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("✏️ Editing", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Language:", GUILayout.Width(110));
        currentLanguage = (GameLanguage)EditorGUILayout.EnumPopup(currentLanguage);
        GUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        // --- GROUP 2: BASE REFERENCE ---
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("📖 Reference", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Show Reference:", GUILayout.Width(110));
        showReferenceView = EditorGUILayout.Toggle(showReferenceView);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Language:", GUILayout.Width(110));
        referenceLanguage = (GameLanguage)EditorGUILayout.EnumPopup(referenceLanguage);
        GUILayout.EndHorizontal();

        
        EditorGUILayout.EndVertical();

        if (EditorGUI.EndChangeCheck())
        {
            GUIUtility.keyboardControl = 0;
            GUIUtility.hotControl = 0;
            EditorGUIUtility.editingTextField = false;
            GUI.FocusControl(null);

            SyncLanguageToAllNodes();
        }

        GUILayout.EndArea();
    }

    private void SyncLanguageToAllNodes()
    {
        DialogueGraph graph = target as DialogueGraph;
        if (graph == null) return;

        foreach (var node in graph.nodes)
        {
            if (node is BaseNode baseNode)
            {
                baseNode.editingLanguage = currentLanguage;
                EditorUtility.SetDirty(baseNode);
            }
        }
        if (NodeEditorWindow.current != null) NodeEditorWindow.current.Repaint();
    }
}
