using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

[CustomNodeEditor(typeof(ChatNode))]
public class ChatNodeEditor : NodeEditor
{
    private static readonly int langCount = System.Enum.GetValues(typeof(GameLanguage)).Length;

    private SerializedObject cachedSerializedObject;
    private SerializedProperty namesProp;
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

        namesProp = serializedObject.FindProperty("localizedSpeakerNames");
        textsProp = serializedObject.FindProperty("localizedTexts");
        audiosProp = serializedObject.FindProperty("localizedAudios");
        editingLangProp = serializedObject.FindProperty("editingLanguage");
        autoAdvanceModeProp = serializedObject.FindProperty("autoAdvanceMode");
        displayDurationProp = serializedObject.FindProperty("displayDuration");
        onEnterProp = serializedObject.FindProperty("onEnter");
        onExitProp = serializedObject.FindProperty("onExit");
    }

    public override int GetWidth() { return 380; }

    public override void OnBodyGUI()
    {
        if (target == null) return;

        // FIXED: Now specifically checks if the new properties are null to force a recache!
        if (cachedSerializedObject != serializedObject || editingLangProp == null || onEnterProp == null)
        {
            CacheProperties();
        }

        serializedObject.Update();
        ChatNode node = target as ChatNode;

        bool forceSave = false;
        EditorGUI.BeginChangeCheck();

        NodeEditorGUILayout.PortField(node.GetPort("enter"));
        NodeEditorGUILayout.PortField(node.GetPort("exit"));
        EditorGUILayout.Space(5);

        if (namesProp != null && namesProp.arraySize != langCount) { namesProp.arraySize = langCount; forceSave = true; }
        if (textsProp != null && textsProp.arraySize != langCount) { textsProp.arraySize = langCount; forceSave = true; }
        if (audiosProp != null && audiosProp.arraySize != langCount) { audiosProp.arraySize = langCount; forceSave = true; }

        DialogueGraphEditor graphEditor = NodeEditorWindow.current.graphEditor as DialogueGraphEditor;
        bool showRef = graphEditor != null && graphEditor.showReferenceView;
        int currentIdx = editingLangProp != null ? editingLangProp.enumValueIndex : 0;

        if (showRef && graphEditor != null)
        {
            int refIdx = (int)graphEditor.referenceLanguage;
            if (refIdx >= 0 && refIdx < textsProp.arraySize)
            {
                string refLangName = ((GameLanguage)refIdx).ToString();
                EditorGUILayout.BeginVertical("HelpBox");
                EditorGUILayout.LabelField($"📖 Reference: {refLangName.ToUpper()}", EditorStyles.miniLabel);
                EditorGUILayout.Space(2);

                GUI.enabled = false;
                EditorGUILayout.TextField(namesProp.GetArrayElementAtIndex(refIdx).stringValue);
                EditorGUILayout.TextArea(textsProp.GetArrayElementAtIndex(refIdx).stringValue, EditorStyles.textArea, GUILayout.MinHeight(40));
                GUI.enabled = true;

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
        }

        if (currentIdx >= 0 && namesProp != null && currentIdx < namesProp.arraySize)
        {
            string langName = ((GameLanguage)currentIdx).ToString();

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField($"✏️ Editing: {langName.ToUpper()}", EditorStyles.miniLabel);
            EditorGUILayout.Space(2);

            NodeEditorGUILayout.PropertyField(namesProp.GetArrayElementAtIndex(currentIdx), new GUIContent("Speaker Name"));
            EditorGUILayout.Space(2);

            SerializedProperty textElement = textsProp.GetArrayElementAtIndex(currentIdx);
            textElement.stringValue = EditorGUILayout.TextArea(textElement.stringValue, EditorStyles.textArea, GUILayout.MinHeight(60));

            EditorGUILayout.Space(2);
            NodeEditorGUILayout.PropertyField(audiosProp.GetArrayElementAtIndex(currentIdx), new GUIContent("Voice Audio"));
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
                if (GUILayout.Button("Sync to Audio", GUILayout.Width(100)))
                {
                    displayDurationProp.floatValue = currentClip.length;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        else if (node.autoAdvanceMode == AutoAdvanceMode.Audio)
        {
            EditorGUILayout.HelpBox("Automatically advances when the Voice Audio finishes.", MessageType.None);
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
