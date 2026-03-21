using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;

[CustomPropertyDrawer(typeof(AudioFilter))]
public class AdvancedFilterSettingsDrawer : PropertyDrawer
{
    private const string searchFolder = "Assets/Templates/AudioFilters";
    private List<AudioFilterPreset> availablePresets;
    private bool isInitialized = false;

    private void Initialize()
    {
        if (!AssetDatabase.IsValidFolder(searchFolder)) return;

        string[] guids = AssetDatabase.FindAssets("t:AudioFilterPreset", new[] { searchFolder });
        availablePresets = new List<AudioFilterPreset>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var preset = AssetDatabase.LoadAssetAtPath<AudioFilterPreset>(path);
            // Only add if the asset is valid and NOT destroyed
            if (preset != null) availablePresets.Add(preset);
        }

        // Sorting: "Default" first, then alphabetical
        availablePresets.Sort((a, b) =>
        {
            if (a == null || b == null) return 0;
            string nameA = a.name.ToLower();
            string nameB = b.name.ToLower();
            if (nameA == "default") return -1;
            if (nameB == "default") return 1;
            return string.Compare(nameA, nameB);
        });

        isInitialized = true;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 1. REFRESH CHECK: Detect destroyed or added/removed templates
        string[] currentGuids = AssetDatabase.FindAssets("t:AudioFilterPreset", new[] { searchFolder });
        bool needsRefresh = !isInitialized || availablePresets == null || currentGuids.Length != availablePresets.Count;

        if (!needsRefresh)
        {
            for (int i = 0; i < availablePresets.Count; i++)
            {
                // Check for "Fake Nulls" (Assets destroyed by overwriting files)
                if (availablePresets[i] == null) { needsRefresh = true; break; }
            }
        }

        if (needsRefresh) Initialize();

        EditorGUI.BeginProperty(position, label, property);
        try
        {
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                int currentIndex = GetMatchingTemplateIndex(property);

                // Button label: Clean name (No numbers/checkmarks)
                string displayLabel = "Custom (Modified)";
                if (currentIndex != -1 && currentIndex < availablePresets.Count && availablePresets[currentIndex] != null)
                {
                    displayLabel = GetHumanReadableName(availablePresets[currentIndex].name);
                }

                Rect dropdownRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(new Rect(dropdownRect.x, dropdownRect.y, EditorGUIUtility.labelWidth, dropdownRect.height), "Active Template");

                Rect buttonRect = new Rect(dropdownRect.x + EditorGUIUtility.labelWidth, dropdownRect.y, dropdownRect.width - EditorGUIUtility.labelWidth, dropdownRect.height);

                if (EditorGUI.DropdownButton(buttonRect, new GUIContent(displayLabel), FocusType.Keyboard))
                {
                    GenericMenu menu = new GenericMenu();
                    if (availablePresets == null || availablePresets.Count == 0)
                    {
                        menu.AddDisabledItem(new GUIContent("No templates found"));
                    }
                    else
                    {
                        SerializedObject so = property.serializedObject;
                        string propPath = property.propertyPath;

                        for (int i = 0; i < availablePresets.Count; i++)
                        {
                            if (availablePresets[i] == null) continue;

                            // Menu labels: Numbered and Checkmarked
                            string menuLabel = $"{i + 1}: {GetHumanReadableName(availablePresets[i].name)}";
                            int index = i;

                            menu.AddItem(new GUIContent(menuLabel), currentIndex == i, () =>
                            {
                                ApplyTemplateFromMenu(so, propPath, availablePresets[index]);
                            });
                        }
                    }
                    menu.DropDown(buttonRect);
                }

                // Draw children properties (sliders/bools)
                float currentY = dropdownRect.y + EditorGUIUtility.singleLineHeight + 6;
                SerializedProperty iterator = property.Copy();
                SerializedProperty endProperty = property.GetEndProperty();
                bool enterChildren = true;

                while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
                {
                    enterChildren = false;
                    float propHeight = EditorGUI.GetPropertyHeight(iterator, true);
                    EditorGUI.PropertyField(new Rect(position.x, currentY, position.width, propHeight), iterator, true);
                    currentY += propHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                // Save Button
                Rect saveBtnRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                if (GUI.Button(saveBtnRect, "Save As New Template..."))
                {
                    SaveCurrentAsTemplate(property);
                }

                EditorGUI.indentLevel--;
            }
        }
        finally
        {
            EditorGUI.EndProperty();
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;
        float height = EditorGUIUtility.singleLineHeight * 2 + 8;
        SerializedProperty iterator = property.Copy();
        SerializedProperty endProperty = property.GetEndProperty();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
        {
            enterChildren = false;
            height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
        }
        return height + EditorGUIUtility.singleLineHeight + 6;
    }

    private int GetMatchingTemplateIndex(SerializedProperty property)
    {
        if (availablePresets == null) return -1;
        for (int i = 0; i < availablePresets.Count; i++)
        {
            if (availablePresets[i] == null) continue;

            // Re-wrap in SerializedObject safely
            using (SerializedObject templateSO = new SerializedObject(availablePresets[i]))
            {
                if (DynamicCompareProperties(property, templateSO.FindProperty("settings"))) return i;
            }
        }
        return -1;
    }

    private void ApplyTemplateFromMenu(SerializedObject activeTargetSO, string propPath, AudioFilterPreset templateAsset)
    {
        if (templateAsset == null) return;
        activeTargetSO.Update();

        using (SerializedObject templateSO = new SerializedObject(templateAsset))
        {
            DynamicCopyProperties(templateSO.FindProperty("settings"), activeTargetSO.FindProperty(propPath));
        }

        activeTargetSO.ApplyModifiedProperties();
    }


    private void SaveCurrentAsTemplate(SerializedProperty property)
    {
        string path = EditorUtility.SaveFilePanelInProject("Save Filter Template", "NewFilterTemplate", "asset", "", searchFolder);
        if (string.IsNullOrEmpty(path)) return;

        AudioFilterPreset newPreset = ScriptableObject.CreateInstance<AudioFilterPreset>();
        newPreset.settings = new AudioFilter();

        // This overwrites the existing asset, destroying the old one
        AssetDatabase.CreateAsset(newPreset, path);

        using (SerializedObject so = new SerializedObject(newPreset))
        {
            DynamicCopyProperties(property, so.FindProperty("settings"));
            so.ApplyModifiedProperties();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Force an immediate reload next frame
        isInitialized = false;

        Debug.Log($"Template saved to: {path}");
    }

    private string GetHumanReadableName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName)) return rawName;
        string cleanName = Regex.Replace(rawName.Replace("_", " ").Replace("-", " "), "([a-z])([A-Z])", "$1 $2");
        string[] words = cleanName.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++) words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
        return string.Join(" ", words);
    }

    private void DynamicCopyProperties(SerializedProperty sourceRoot, SerializedProperty targetRoot)
    {
        var iterator = sourceRoot.Copy();
        var end = sourceRoot.GetEndProperty();
        while (iterator.NextVisible(true) && !SerializedProperty.EqualContents(iterator, end))
        {
            var targetProp = targetRoot.FindPropertyRelative(iterator.name);
            if (targetProp != null)
            {
                if (iterator.propertyType == SerializedPropertyType.Boolean) targetProp.boolValue = iterator.boolValue;
                else if (iterator.propertyType == SerializedPropertyType.Float) targetProp.floatValue = iterator.floatValue;
            }
        }
    }

    private bool DynamicCompareProperties(SerializedProperty rootA, SerializedProperty rootB)
    {
        var iterator = rootA.Copy();
        var end = rootA.GetEndProperty();
        while (iterator.NextVisible(true) && !SerializedProperty.EqualContents(iterator, end))
        {
            var propB = rootB.FindPropertyRelative(iterator.name);
            if (propB != null)
            {
                if (iterator.propertyType == SerializedPropertyType.Boolean && iterator.boolValue != propB.boolValue) return false;
                if (iterator.propertyType == SerializedPropertyType.Float && !Mathf.Approximately(iterator.floatValue, propB.floatValue)) return false;
            }
        }
        return true;
    }
}
