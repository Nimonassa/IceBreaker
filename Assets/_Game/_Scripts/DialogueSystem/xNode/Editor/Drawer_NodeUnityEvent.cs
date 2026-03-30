using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;
using XNode;

namespace XNodeEditor
{
    [CustomPropertyDrawer(typeof(xNodeUnityEventAttribute))]
    public class NodeUnityEventDrawer : PropertyDrawer
    {
        private Dictionary<string, ReorderableList> listCache = new Dictionary<string, ReorderableList>();

        // Intelligent whitelist for useful engine methods
        private static readonly HashSet<string> WhitelistedBaseMethods = new HashSet<string> {
            "SetActive", "SendMessage", "SendMessageUpwards", "BroadcastMessage",
            "StopCoroutine", "StopAllCoroutines", "CancelInvoke"
        };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (NodeEditorWindow.current == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            Rect indentedPosition = EditorGUI.IndentedRect(position);
            int oldIndent = EditorGUI.indentLevel;

            try
            {
                ReorderableList list = GetList(property);
                EditorGUI.indentLevel = 0;
                list.DoList(indentedPosition);
            }
            finally
            {
                // Restore GUI state to prevent Clip/Layout errors
                EditorGUI.indentLevel = oldIndent;
                EditorGUI.EndProperty();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (NodeEditorWindow.current == null) return EditorGUI.GetPropertyHeight(property, label, true);
            return GetList(property).GetHeight();
        }

        private ReorderableList GetList(SerializedProperty property)
        {
            string path = property.propertyPath;
            if (listCache.TryGetValue(path, out ReorderableList existingList)) return existingList;

            SerializedProperty calls = property.FindPropertyRelative("m_PersistentCalls.m_Calls");
            ReorderableList list = new ReorderableList(property.serializedObject, calls, true, true, true, true);

            list.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, property.displayName);

            list.onAddCallback = (ReorderableList l) =>
            {
                int index = l.serializedProperty.arraySize;
                l.serializedProperty.arraySize++;
                l.index = index;
                SerializedProperty element = l.serializedProperty.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("m_CallState").enumValueIndex = 2; // Default to Runtime Only
            };

            list.elementHeightCallback = (int index) =>
            {
                if (index < 0 || index >= calls.arraySize) return 0;
                SerializedProperty call = calls.GetArrayElementAtIndex(index);
                SerializedProperty mode = call.FindPropertyRelative("m_Mode");
                return (mode != null && mode.enumValueIndex >= 2) ? EditorGUIUtility.singleLineHeight * 2 + 6 : EditorGUIUtility.singleLineHeight + 4;
            };

            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index < 0 || index >= calls.arraySize) return;

                SerializedProperty call = calls.GetArrayElementAtIndex(index);
                SerializedProperty targetProp = call.FindPropertyRelative("m_Target");
                SerializedProperty methodNameProp = call.FindPropertyRelative("m_MethodName");
                SerializedProperty modeProp = call.FindPropertyRelative("m_Mode");

                rect.y += 2;

                // 40/60 Ratio for better label fit
                float targetWidth = rect.width * 0.4f;
                float functionWidth = rect.width * 0.6f;

                Rect targetRect = new Rect(rect.x, rect.y, targetWidth - 2, EditorGUIUtility.singleLineHeight);
                Rect functionRect = new Rect(rect.x + targetWidth + 2, rect.y, functionWidth - 2, EditorGUIUtility.singleLineHeight);

                EditorGUI.PropertyField(targetRect, targetProp, GUIContent.none);

                // LABEL: ComponentName.MethodName
                string displayLabel = "No Function";
                bool isValid = true;
                int currentMode = modeProp != null ? modeProp.enumValueIndex : 1;

                if (targetProp.objectReferenceValue != null && !string.IsNullOrEmpty(methodNameProp.stringValue))
                {
                    string targetName = targetProp.objectReferenceValue.GetType().Name;
                    string methodDisplay = methodNameProp.stringValue;
                    if (methodDisplay.StartsWith("set_")) methodDisplay = methodDisplay.Substring(4);

                    displayLabel = $"{targetName}.{methodDisplay}";
                    isValid = IsMethodValid(targetProp.objectReferenceValue, methodNameProp.stringValue, currentMode);
                    if (!isValid) displayLabel = "<Missing> " + displayLabel;
                }
                else if (!string.IsNullOrEmpty(methodNameProp.stringValue))
                {
                    displayLabel = "<Missing Object>";
                    isValid = false;
                }

                Vector2 localMouse = Event.current.mousePosition;
                GUIStyle buttonStyle = new GUIStyle(EditorStyles.popup);
                if (!isValid) buttonStyle.normal.textColor = Color.red;

                if (EditorGUI.DropdownButton(functionRect, new GUIContent(displayLabel), FocusType.Passive, buttonStyle))
                {
                    if (targetProp.objectReferenceValue != null)
                    {
                        float zoom = NodeEditorWindow.current.zoom;
                        Vector2 localOffset = functionRect.position - localMouse;
                        Vector2 windowOffset = localOffset / zoom;
                        Vector2 windowSize = functionRect.size / zoom;

                        UnityEngine.Object targetObj = targetProp.objectReferenceValue;
                        NodeEditorWindow.current.onLateGUI += () => ShowFunctionMenu(call, targetObj, windowOffset, windowSize);
                    }
                }

                if (modeProp != null && modeProp.enumValueIndex >= 2)
                {
                    Rect argRect = new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + 2, rect.width, EditorGUIUtility.singleLineHeight);
                    SerializedProperty argsProp = call.FindPropertyRelative("m_Arguments");
                    switch (modeProp.enumValueIndex)
                    {
                        case 2: EditorGUI.PropertyField(argRect, argsProp.FindPropertyRelative("m_ObjectArgument"), GUIContent.none); break;
                        case 3: EditorGUI.PropertyField(argRect, argsProp.FindPropertyRelative("m_IntArgument"), GUIContent.none); break;
                        case 4: EditorGUI.PropertyField(argRect, argsProp.FindPropertyRelative("m_FloatArgument"), GUIContent.none); break;
                        case 5: EditorGUI.PropertyField(argRect, argsProp.FindPropertyRelative("m_StringArgument"), GUIContent.none); break;
                        case 6: EditorGUI.PropertyField(argRect, argsProp.FindPropertyRelative("m_BoolArgument"), GUIContent.none); break;
                    }
                }
            };

            listCache[path] = list;
            return list;
        }

        private static bool IsMethodValid(UnityEngine.Object target, string methodName, int mode)
        {
            if (target == null || string.IsNullOrEmpty(methodName)) return false;
            MethodInfo[] methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
            foreach (MethodInfo m in methods)
            {
                if (m.Name == methodName)
                {
                    if (!IsWithinIntelligentScope(m)) continue;
                    ParameterInfo[] p = m.GetParameters();
                    if (p.Length > 1) continue;
                    if (p.Length == 0) { if (mode == 1) return true; }
                    else if (p.Length == 1)
                    {
                        Type pType = p[0].ParameterType;
                        if (pType == typeof(int) && mode == 3) return true;
                        if (pType == typeof(float) && mode == 4) return true;
                        if (pType == typeof(string) && mode == 5) return true;
                        if (pType == typeof(bool) && mode == 6) return true;
                        if (typeof(UnityEngine.Object).IsAssignableFrom(pType) && mode == 2) return true;
                    }
                }
            }
            return false;
        }

        private static bool IsWithinIntelligentScope(MethodInfo m)
        {
            if (m.ReturnType != typeof(void)) return false;
            Type declType = m.DeclaringType;
            if (declType.Namespace == null || !declType.Namespace.StartsWith("UnityEngine")) return true;
            bool isSetter = m.IsSpecialName && m.Name.StartsWith("set_");
            bool isWhitelisted = WhitelistedBaseMethods.Contains(m.Name);
            return isSetter || isWhitelisted;
        }

        public static void ShowFunctionMenu(SerializedProperty callProperty, UnityEngine.Object targetObj, Vector2 windowOffset, Vector2 windowSize)
        {
            GenericMenu menu = new GenericMenu();

            // Checkmark logic: Determine if 'No Function' is selected
            string currentMethodName = callProperty.FindPropertyRelative("m_MethodName").stringValue;
            bool isNoFunction = string.IsNullOrEmpty(currentMethodName);
            menu.AddItem(new GUIContent("No Function"), isNoFunction, () => SetFunction(callProperty, null, null));

            if (targetObj != null)
            {
                GameObject go = targetObj as GameObject;
                if (go == null && targetObj is Component comp) go = comp.gameObject;
                if (go != null)
                {
                    menu.AddSeparator("");
                    AddMethodsToMenu(menu, callProperty, go, "GameObject");
                    Component[] components = go.GetComponents<Component>();
                    foreach (Component c in components) { if (c != null) AddMethodsToMenu(menu, callProperty, c, c.GetType().Name); }
                }
            }
            Vector2 windowMouse = Event.current.mousePosition;
            Rect buttonRect = new Rect(windowMouse + windowOffset, windowSize);
            menu.DropDown(buttonRect);
        }

        private static void AddMethodsToMenu(GenericMenu menu, SerializedProperty callProperty, UnityEngine.Object target, string menuCategory)
        {
            // Get current selection state for checkmarks
            string currentMethodName = callProperty.FindPropertyRelative("m_MethodName").stringValue;
            UnityEngine.Object currentTarget = callProperty.FindPropertyRelative("m_Target").objectReferenceValue;

            MethodInfo[] methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);

            // INTELLIGENT SORTING:
            // 1. Primary Sort: Methods declared in this class come FIRST.
            // 2. Secondary Sort: Everything sorted Alphabetically.
            var sortedMethods = methods
                .Where(m => IsWithinIntelligentScope(m))
                .OrderByDescending(m => m.DeclaringType == target.GetType())
                .ThenBy(m => m.Name)
                .ToList();

            foreach (MethodInfo method in sortedMethods)
            {
                if (method.IsSpecialName && !method.Name.StartsWith("set_")) continue;
                if (method.IsGenericMethod) continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length > 1) continue;

                if (parameters.Length == 1)
                {
                    Type pType = parameters[0].ParameterType;
                    if (pType != typeof(int) && pType != typeof(float) && pType != typeof(string) && pType != typeof(bool) && !typeof(UnityEngine.Object).IsAssignableFrom(pType)) continue;
                }

                string paramTypeName = parameters.Length == 1 ? parameters[0].ParameterType.Name : "";
                string methodNameDisplay = string.IsNullOrEmpty(paramTypeName) ? method.Name : $"{method.Name} ({paramTypeName})";
                if (method.Name.StartsWith("set_")) methodNameDisplay = method.Name.Substring(4) + " " + (string.IsNullOrEmpty(paramTypeName) ? "" : $"({paramTypeName})");

                string fullMenuPath = $"{menuCategory}/{methodNameDisplay}";
                MethodInfo capturedMethod = method;

                // CHECKMARK: Selected if BOTH the target and method name match
                bool isSelected = (target == currentTarget && method.Name == currentMethodName);
                menu.AddItem(new GUIContent(fullMenuPath), isSelected, () => SetFunction(callProperty, target, capturedMethod));
            }
        }

        private static void SetFunction(SerializedProperty callProperty, UnityEngine.Object actualTarget, MethodInfo method)
        {
            callProperty.FindPropertyRelative("m_MethodName").stringValue = method != null ? method.Name : "";
            if (actualTarget != null) callProperty.FindPropertyRelative("m_Target").objectReferenceValue = actualTarget;
            SerializedProperty modeProp = callProperty.FindPropertyRelative("m_Mode");
            if (method == null) modeProp.enumValueIndex = 1;
            else
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 0) modeProp.enumValueIndex = 1;
                else
                {
                    Type pType = parameters[0].ParameterType;
                    if (pType == typeof(int)) modeProp.enumValueIndex = 3;
                    else if (pType == typeof(float)) modeProp.enumValueIndex = 4;
                    else if (pType == typeof(string)) modeProp.enumValueIndex = 5;
                    else if (pType == typeof(bool)) modeProp.enumValueIndex = 6;
                    else if (typeof(UnityEngine.Object).IsAssignableFrom(pType))
                    {
                        modeProp.enumValueIndex = 2;
                        callProperty.FindPropertyRelative("m_Arguments.m_ObjectArgumentAssemblyTypeName").stringValue = pType.AssemblyQualifiedName;
                    }
                    else modeProp.enumValueIndex = 1;
                }
            }
            callProperty.serializedObject.ApplyModifiedProperties();
            callProperty.serializedObject.Update();
        }
    }
}
