using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DialogueChoice))]
public class DialogueChoiceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ChoiceNode node = property.serializedObject.targetObject as ChoiceNode;
        if (node == null) return;

        int langIndex = (int)node.editingLanguage;
        // Correctly get the current enum count
        int langCount = System.Enum.GetValues(typeof(GameLanguage)).Length;
        SerializedProperty textsProp = property.FindPropertyRelative("localizedChoiceTexts");

        // SAFETY RESIZE: If the array is too small for the current language, fix it immediately
        if (textsProp != null && textsProp.arraySize < langCount)
        {
            textsProp.arraySize = langCount;
        }
        
        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        string foldoutLabel = "Choice";
        if (textsProp != null && langIndex < textsProp.arraySize)
        {
            string preview = textsProp.GetArrayElementAtIndex(langIndex).stringValue;
            if (!string.IsNullOrEmpty(preview)) foldoutLabel = preview.Length > 30 ? preview.Substring(0, 30) + "..." : preview;
        }

        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, foldoutLabel, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float y = position.y + EditorGUIUtility.singleLineHeight + 2;

            // 2. Multi-line Text Field
            if (textsProp != null && langIndex < textsProp.arraySize)
            {
                Rect labelRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(labelRect, "Choice Text (" + node.editingLanguage + ")");
                y += EditorGUIUtility.singleLineHeight;

                Rect areaRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight * 3);
                SerializedProperty textElement = textsProp.GetArrayElementAtIndex(langIndex);
                textElement.stringValue = EditorGUI.TextArea(areaRect, textElement.stringValue);
                y += (EditorGUIUtility.singleLineHeight * 3) + 5;
            }

            // 3. UnityEvent Field
            SerializedProperty eventProp = property.FindPropertyRelative("onChoicePicked");
            if (eventProp != null)
            {
                float eventHeight = EditorGUI.GetPropertyHeight(eventProp);
                Rect eventRect = new Rect(position.x, y, position.width, eventHeight);
                EditorGUI.PropertyField(eventRect, eventProp, new GUIContent("On Picked"));
            }
            EditorGUI.indentLevel--;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;

        // Header + Text Label + Text Area (3 lines) + Event Height
        float height = EditorGUIUtility.singleLineHeight * 2; // Header + Label
        height += EditorGUIUtility.singleLineHeight * 3; // TextArea

        SerializedProperty eventProp = property.FindPropertyRelative("onChoicePicked");
        if (eventProp != null) height += EditorGUI.GetPropertyHeight(eventProp);

        return height + 15; // Padding
    }
}
