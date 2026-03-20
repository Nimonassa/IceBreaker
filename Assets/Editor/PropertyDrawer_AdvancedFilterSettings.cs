using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions; // Added for string formatting

[CustomPropertyDrawer(typeof(AdvancedFilterSettings))]
public class AdvancedFilterSettingsDrawer : PropertyDrawer
{
    private const string searchFolder = "Assets/Templates/AudioFilters";

    private List<AdvancedFilterPreset> availablePresets;
    private bool isInitialized = false;

    private void Initialize(string[] guids)
    {
        availablePresets = new List<AdvancedFilterPreset>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            availablePresets.Add(AssetDatabase.LoadAssetAtPath<AdvancedFilterPreset>(path));
        }

        isInitialized = true;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Lightweight auto-refresh: Check if the number of templates in the folder changed
        string[] currentGuids = AssetDatabase.FindAssets("t:AdvancedFilterPreset", new[] { searchFolder });
        if (!isInitialized || availablePresets == null || currentGuids.Length != availablePresets.Count)
        {
            Initialize(currentGuids);
        }

        EditorGUI.BeginProperty(position, label, property);

        // 1. Draw the main Foldout
        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            // 2. Reactively check if our current values match any known template (-1 means no match)
            int currentIndex = GetMatchingTemplateIndex(property);

            // Apply formatting to the main dropdown display label
            string displayLabel = currentIndex == -1 ? "Custom (Modified)" : GetHumanReadableName(availablePresets[currentIndex].name);

            Rect dropdownRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, EditorGUIUtility.singleLineHeight);

            Rect prefixRect = new Rect(dropdownRect.x, dropdownRect.y, EditorGUIUtility.labelWidth, dropdownRect.height);
            EditorGUI.LabelField(prefixRect, "Active Template");

            Rect buttonRect = new Rect(dropdownRect.x + EditorGUIUtility.labelWidth, dropdownRect.y, dropdownRect.width - EditorGUIUtility.labelWidth, dropdownRect.height);

            if (EditorGUI.DropdownButton(buttonRect, new GUIContent(displayLabel), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();

                if (availablePresets.Count == 0)
                {
                    menu.AddDisabledItem(new GUIContent("No templates found in folder"));
                }
                else
                {
                    SerializedObject so = property.serializedObject;
                    string propPath = property.propertyPath;

                    for (int i = 0; i < availablePresets.Count; i++)
                    {
                        int index = i;
                        // Apply formatting to the items inside the dropdown list
                        string formattedName = GetHumanReadableName(availablePresets[i].name);

                        menu.AddItem(new GUIContent(formattedName), currentIndex == i, () =>
                        {
                            ApplyTemplateFromMenu(so, propPath, availablePresets[index]);
                        });
                    }
                }
                menu.DropDown(buttonRect);
            }

            // 3. Draw all the normal sliders and flags automatically
            float currentY = dropdownRect.y + EditorGUIUtility.singleLineHeight + 6;

            SerializedProperty iterator = property.Copy();
            SerializedProperty endProperty = property.GetEndProperty();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                enterChildren = false;
                float propHeight = EditorGUI.GetPropertyHeight(iterator, true);
                Rect propRect = new Rect(position.x, currentY, position.width, propHeight);

                EditorGUI.PropertyField(propRect, iterator, true);
                currentY += propHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            // 4. Save Button (Native Unity Popup)
            Rect saveBtnRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(saveBtnRect, "Save As New Template..."))
            {
                SaveCurrentAsTemplate(property);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
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

        height += EditorGUIUtility.singleLineHeight + 6;
        return height;
    }

    // ==========================================
    // --- DYNAMIC HELPER METHODS (No Hardcoding) ---
    // ==========================================

    private void SaveCurrentAsTemplate(SerializedProperty property)
    {
        if (!AssetDatabase.IsValidFolder(searchFolder))
        {
            Debug.LogError($"The folder {searchFolder} does not exist! Please ensure it is created exactly as spelled.");
            return;
        }

        string path = EditorUtility.SaveFilePanelInProject("Save Audio Filter Template", "NewFilterTemplate", "asset", "Enter a name for the new template.", searchFolder);
        if (string.IsNullOrEmpty(path)) return;

        // 1. Create the base asset
        AdvancedFilterPreset newPreset = ScriptableObject.CreateInstance<AdvancedFilterPreset>();
        newPreset.settings = new AdvancedFilterSettings();
        AssetDatabase.CreateAsset(newPreset, path);

        // 2. Dynamically copy all variables from the inspector to the new asset
        SerializedObject newPresetSO = new SerializedObject(newPreset);
        SerializedProperty newSettingsProp = newPresetSO.FindProperty("settings");

        DynamicCopyProperties(property, newSettingsProp);

        newPresetSO.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        Debug.Log($"Successfully saved new audio filter template: {path}");
    }

    private int GetMatchingTemplateIndex(SerializedProperty property)
    {
        for (int i = 0; i < availablePresets.Count; i++)
        {
            if (availablePresets[i] == null) continue;

            SerializedObject templateSO = new SerializedObject(availablePresets[i]);
            SerializedProperty templateSettingsProp = templateSO.FindProperty("settings");

            // If the dynamic check returns true, we have a perfect match
            if (DynamicCompareProperties(property, templateSettingsProp))
            {
                return i;
            }
        }
        return -1;
    }

    private void ApplyTemplateFromMenu(SerializedObject activeTargetSO, string propPath, AdvancedFilterPreset templateAsset)
    {
        activeTargetSO.Update();
        SerializedProperty activeProperty = activeTargetSO.FindProperty(propPath);

        SerializedObject templateSO = new SerializedObject(templateAsset);
        SerializedProperty templateSettingsProp = templateSO.FindProperty("settings");

        // Dynamically copy all variables from the template into the active inspector
        DynamicCopyProperties(templateSettingsProp, activeProperty);

        activeTargetSO.ApplyModifiedProperties();
    }

    // --- Format Helper ---

    private string GetHumanReadableName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName)) return rawName;

        // 1. Replace underscores and dashes with spaces
        string cleanName = rawName.Replace("_", " ").Replace("-", " ");

        // 2. Add spaces before Capital letters that follow lower case letters (handles camelCase and PascalCase)
        cleanName = Regex.Replace(cleanName, "([a-z])([A-Z])", "$1 $2");

        // 3. Capitalize the first letter of every word
        string[] words = cleanName.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
            }
        }

        return string.Join(" ", words);
    }

    // --- Core Iterators ---

    private void DynamicCopyProperties(SerializedProperty sourceRoot, SerializedProperty targetRoot)
    {
        SerializedProperty iterator = sourceRoot.Copy();
        SerializedProperty endProperty = sourceRoot.GetEndProperty();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
        {
            enterChildren = false;
            SerializedProperty targetProp = targetRoot.FindPropertyRelative(iterator.name);

            if (targetProp != null)
            {
                switch (iterator.propertyType)
                {
                    case SerializedPropertyType.Boolean: targetProp.boolValue = iterator.boolValue; break;
                    case SerializedPropertyType.Float: targetProp.floatValue = iterator.floatValue; break;
                }
            }
        }
    }

    private bool DynamicCompareProperties(SerializedProperty rootA, SerializedProperty rootB)
    {
        SerializedProperty iterator = rootA.Copy();
        SerializedProperty endProperty = rootA.GetEndProperty();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
        {
            enterChildren = false;
            SerializedProperty propB = rootB.FindPropertyRelative(iterator.name);

            if (propB != null)
            {
                switch (iterator.propertyType)
                {
                    case SerializedPropertyType.Boolean:
                        if (iterator.boolValue != propB.boolValue) return false;
                        break;
                    case SerializedPropertyType.Float:
                        if (!Mathf.Approximately(iterator.floatValue, propB.floatValue)) return false;
                        break;
                }
            }
        }
        return true;
    }
}
