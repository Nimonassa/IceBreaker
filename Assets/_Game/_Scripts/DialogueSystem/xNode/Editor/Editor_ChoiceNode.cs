using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

[CustomNodeEditor(typeof(ChoiceNode))]
public class ChoiceNodeEditor : NodeEditor
{
    private static readonly int langCount = System.Enum.GetValues(typeof(GameLanguage)).Length;
    private int lastChoiceCount = -1;

    private SerializedObject cachedSerializedObject;
    private SerializedProperty editingLangProp;
    private SerializedProperty namesProp;
    private SerializedProperty textsProp;
    private SerializedProperty audiosProp;
    private SerializedProperty choicesProp;
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

        editingLangProp = serializedObject.FindProperty("editingLanguage");
        namesProp = serializedObject.FindProperty("localizedSpeakerNames");
        textsProp = serializedObject.FindProperty("localizedPromptTexts");
        audiosProp = serializedObject.FindProperty("localizedPromptAudios");
        choicesProp = serializedObject.FindProperty("choices");
        onEnterProp = serializedObject.FindProperty("onEnter");
        onExitProp = serializedObject.FindProperty("onExit");
    }

    public override int GetWidth() => 380;

    public override void OnBodyGUI()
    {
        if (target == null) return;

        // FIXED: Added null safety for caching
        if (cachedSerializedObject != serializedObject || editingLangProp == null || onEnterProp == null)
        {
            CacheProperties();
        }

        serializedObject.Update();
        ChoiceNode node = target as ChoiceNode;

        bool forceSave = false;
        EditorGUI.BeginChangeCheck();

        NodeEditorGUILayout.PortField(node.GetPort("enter"));

        int currentIdx = editingLangProp != null ? editingLangProp.enumValueIndex : 0;

        if (namesProp != null && namesProp.arraySize != langCount) { namesProp.arraySize = langCount; forceSave = true; }
        if (textsProp != null && textsProp.arraySize != langCount) { textsProp.arraySize = langCount; forceSave = true; }
        if (audiosProp != null && audiosProp.arraySize != langCount) { audiosProp.arraySize = langCount; forceSave = true; }

        DialogueGraphEditor graphEditor = NodeEditorWindow.current.graphEditor as DialogueGraphEditor;
        bool showRef = graphEditor != null && graphEditor.showReferenceView;

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
                if (namesProp != null) EditorGUILayout.TextField(namesProp.GetArrayElementAtIndex(refIdx).stringValue);
                if (textsProp != null) EditorGUILayout.TextArea(textsProp.GetArrayElementAtIndex(refIdx).stringValue, EditorStyles.textArea, GUILayout.MinHeight(40));
                GUI.enabled = true;

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
        }

        EditorGUILayout.BeginVertical(GUI.skin.box);
        string langName = currentIdx >= 0 ? ((GameLanguage)currentIdx).ToString() : "Unknown";
        EditorGUILayout.LabelField($"✏️ Editing Prompt: {langName.ToUpper()}", EditorStyles.miniLabel);
        EditorGUILayout.Space(2);

        if (namesProp != null) NodeEditorGUILayout.PropertyField(namesProp.GetArrayElementAtIndex(currentIdx), new GUIContent("Speaker Name"));
        EditorGUILayout.Space(2);
        if (textsProp != null)
        {
            SerializedProperty textElement = textsProp.GetArrayElementAtIndex(currentIdx);
            textElement.stringValue = EditorGUILayout.TextArea(textElement.stringValue, EditorStyles.textArea, GUILayout.MinHeight(60));
        }
        EditorGUILayout.Space(2);
        if (audiosProp != null) NodeEditorGUILayout.PropertyField(audiosProp.GetArrayElementAtIndex(currentIdx), new GUIContent("Prompt Audio"));
        EditorGUILayout.EndVertical();

        if (choicesProp != null && choicesProp.isArray)
        {
            if (choicesProp.arraySize != lastChoiceCount)
            {
                for (int i = 0; i < choicesProp.arraySize; i++)
                {
                    SerializedProperty choiceElement = choicesProp.GetArrayElementAtIndex(i);
                    SerializedProperty choiceTextsArr = choiceElement.FindPropertyRelative("localizedChoiceTexts");
                    if (choiceTextsArr != null && choiceTextsArr.arraySize != langCount)
                    {
                        choiceTextsArr.arraySize = langCount;
                    }
                }
                lastChoiceCount = choicesProp.arraySize;
            }
        }

        EditorGUILayout.Space(10);
        if (choicesProp != null) NodeEditorGUILayout.DynamicPortList(node.choicesFieldName, typeof(DialogueChoice), serializedObject, NodePort.IO.Output, Node.ConnectionType.Override);

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
