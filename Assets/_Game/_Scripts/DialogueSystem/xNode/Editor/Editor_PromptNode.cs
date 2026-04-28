using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

[CustomNodeEditor(typeof(PromptNode))]
public class PromptNodeEditor : NodeEditor
{
    private static readonly int langCount = System.Enum.GetValues(typeof(GameLanguage)).Length;

    private SerializedObject cachedSerializedObject;
    private SerializedProperty promptTypeProp;
    private SerializedProperty textsProp;
    private SerializedProperty audiosProp;
    private SerializedProperty editingLangProp;
    private SerializedProperty autoAdvanceModeProp;
    private SerializedProperty displayDurationProp;
    private SerializedProperty onEnterProp;
    private SerializedProperty onExitProp;

    public override void OnCreate()
    {
        base.OnCreate();
        CacheProperties();
    }

    private void CacheProperties()
    {
        cachedSerializedObject = serializedObject;
        if (serializedObject == null) return;

        promptTypeProp = serializedObject.FindProperty("promptType");
        textsProp = serializedObject.FindProperty("localizedTexts");
        audiosProp = serializedObject.FindProperty("localizedAudios");
        editingLangProp = serializedObject.FindProperty("editingLanguage");
        autoAdvanceModeProp = serializedObject.FindProperty("autoAdvanceMode");
        displayDurationProp = serializedObject.FindProperty("displayDuration");
        onEnterProp = serializedObject.FindProperty("onEnter");
        onExitProp = serializedObject.FindProperty("onExit");
    }

    public override int GetWidth() { return 350; }

    public override Color GetTint()
    {
        PromptNode node = target as PromptNode;
        if (node == null) return base.GetTint();

        switch (node.promptType)
        {
            case PromptType.Success: return new Color(0.2f, 0.4f, 0.2f);
            case PromptType.Warning: return new Color(0.4f, 0.35f, 0.1f);
            case PromptType.Failure: return new Color(0.4f, 0.2f, 0.2f);
            default: return base.GetTint();
        }
    }

    public override void OnBodyGUI()
    {
        if (target == null) return;

        // FIXED: Added null safety for caching
        if (cachedSerializedObject != serializedObject || editingLangProp == null || onEnterProp == null)
        {
            CacheProperties();
        }

        serializedObject.Update();
        PromptNode node = target as PromptNode;

        bool forceSave = false;
        EditorGUI.BeginChangeCheck();

        NodeEditorGUILayout.PortField(node.GetPort("enter"));
        NodeEditorGUILayout.PortField(node.GetPort("exit"));

        EditorGUILayout.Space(5);
        if (promptTypeProp != null) NodeEditorGUILayout.PropertyField(promptTypeProp);
        EditorGUILayout.Space(5);

        if (textsProp != null && textsProp.arraySize != langCount) { textsProp.arraySize = langCount; forceSave = true; }
        if (audiosProp != null && audiosProp.arraySize != langCount) { audiosProp.arraySize = langCount; forceSave = true; }

        DialogueGraphEditor graphEditor = NodeEditorWindow.current.graphEditor as DialogueGraphEditor;
        bool showRef = graphEditor != null && graphEditor.showReferenceView;
        int currentIdx = editingLangProp != null ? editingLangProp.enumValueIndex : 0;

        if (showRef && graphEditor != null)
        {
            int refIdx = (int)graphEditor.referenceLanguage;
            if (refIdx >= 0 && textsProp != null && refIdx < textsProp.arraySize)
            {
                string refLangName = ((GameLanguage)refIdx).ToString();
                EditorGUILayout.BeginVertical("HelpBox");
                EditorGUILayout.LabelField($"📖 Reference: {refLangName.ToUpper()}", EditorStyles.miniLabel);
                EditorGUILayout.Space(2);

                GUI.enabled = false;
                EditorGUILayout.TextArea(textsProp.GetArrayElementAtIndex(refIdx).stringValue, EditorStyles.textArea, GUILayout.MinHeight(40));
                GUI.enabled = true;

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
        }

        if (currentIdx >= 0 && textsProp != null && currentIdx < textsProp.arraySize)
        {
            string langName = ((GameLanguage)currentIdx).ToString();

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField($"✏️ Editing: {langName.ToUpper()}", EditorStyles.miniLabel);
            EditorGUILayout.Space(2);

            SerializedProperty textElement = textsProp.GetArrayElementAtIndex(currentIdx);
            textElement.stringValue = EditorGUILayout.TextArea(textElement.stringValue, EditorStyles.textArea, GUILayout.MinHeight(60));

            EditorGUILayout.Space(2);
            NodeEditorGUILayout.PropertyField(audiosProp.GetArrayElementAtIndex(currentIdx), new GUIContent("Prompt Audio"));
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Timing & Pacing", EditorStyles.boldLabel);
        if (autoAdvanceModeProp != null) NodeEditorGUILayout.PropertyField(autoAdvanceModeProp, new GUIContent("Advance Mode"));

        if (node.autoAdvanceMode == AutoAdvanceMode.Timer)
        {
            EditorGUILayout.BeginHorizontal();
            if (displayDurationProp != null) NodeEditorGUILayout.PropertyField(displayDurationProp, new GUIContent("Wait Time (s)"));

            AudioClip currentClip = node.GetAudio((GameLanguage)currentIdx);
            if (currentClip != null)
            {
                if (GUILayout.Button("Sync", GUILayout.Width(60)))
                {
                    displayDurationProp.floatValue = currentClip.length;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(5);
        
        // FIXED: Safe drawing logic
        if (onEnterProp != null) NodeEditorGUILayout.PropertyField(onEnterProp);
        if (onExitProp != null) NodeEditorGUILayout.PropertyField(onExitProp);

        if (EditorGUI.EndChangeCheck() || forceSave)
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}