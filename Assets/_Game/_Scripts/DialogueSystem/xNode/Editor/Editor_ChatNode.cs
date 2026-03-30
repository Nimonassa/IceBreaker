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
    private SerializedProperty autoAdvanceModeProp; // Updated name
    private SerializedProperty displayDurationProp;
    private SerializedProperty onNodeTriggeredProp;

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
        onNodeTriggeredProp = serializedObject.FindProperty("onNodeTriggered");
    }

    public override int GetWidth() { return 380; } // Increased width for better button fit

    public override void OnBodyGUI()
    {
        if (target == null) return;

        if (cachedSerializedObject != serializedObject || editingLangProp == null)
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

        // Safety array resizing
        if (namesProp != null && namesProp.arraySize != langCount) { namesProp.arraySize = langCount; forceSave = true; }
        if (textsProp != null && textsProp.arraySize != langCount) { textsProp.arraySize = langCount; forceSave = true; }
        if (audiosProp != null && audiosProp.arraySize != langCount) { audiosProp.arraySize = langCount; forceSave = true; }

        DialogueGraphEditor graphEditor = NodeEditorWindow.current.graphEditor as DialogueGraphEditor;
        bool showRef = graphEditor != null && graphEditor.showReferenceView;
        int currentIdx = editingLangProp.enumValueIndex;

        // --- REFERENCE VIEW ---
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

        // --- ACTIVE EDITING VIEW ---
        if (currentIdx >= 0 && currentIdx < namesProp.arraySize)
        {
            string langName = ((GameLanguage)currentIdx).ToString();

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField($"✏️ Editing: {langName.ToUpper()}", EditorStyles.miniLabel);
            EditorGUILayout.Space(2);

            NodeEditorGUILayout.PropertyField(namesProp.GetArrayElementAtIndex(currentIdx), new GUIContent("Speaker Name"));
            EditorGUILayout.Space(2);

            // Localized Text Area
            SerializedProperty textElement = textsProp.GetArrayElementAtIndex(currentIdx);
            textElement.stringValue = EditorGUILayout.TextArea(textElement.stringValue, EditorStyles.textArea, GUILayout.MinHeight(60));

            EditorGUILayout.Space(2);
            NodeEditorGUILayout.PropertyField(audiosProp.GetArrayElementAtIndex(currentIdx), new GUIContent("Voice Audio"));
            EditorGUILayout.EndVertical();
        }

        // --- TIMING & PACING ---
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Timing & Pacing", EditorStyles.boldLabel);
        NodeEditorGUILayout.PropertyField(autoAdvanceModeProp, new GUIContent("Advance Mode"));

        if (node.autoAdvanceMode == AutoAdvanceMode.Timer)
        {
            EditorGUILayout.BeginHorizontal();
            NodeEditorGUILayout.PropertyField(displayDurationProp, new GUIContent("Wait Time (s)"));

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
        NodeEditorGUILayout.PropertyField(onNodeTriggeredProp);

        if (EditorGUI.EndChangeCheck() || forceSave)
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
