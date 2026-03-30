using System;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace XNodeEditor
{
    [CustomPropertyDrawer(typeof(xNodeEnumAttribute))]
    public class NodeEnumDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (NodeEditorWindow.current == null || property.serializedObject.targetObject == null)
            {
                property.enumValueIndex = EditorGUI.Popup(position, label.text, property.enumValueIndex, property.enumDisplayNames);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            float labelWidth = EditorGUIUtility.labelWidth;
            Rect labelRect = new Rect(position.x, position.y, labelWidth, position.height);
            Rect buttonRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, position.height);

            GUI.Label(labelRect, label, EditorStyles.label);

            string enumName = property.enumValueIndex >= 0 && property.enumValueIndex < property.enumDisplayNames.Length
                ? property.enumDisplayNames[property.enumValueIndex] : "";

            if (EditorGUI.DropdownButton(buttonRect, new GUIContent(enumName), FocusType.Passive))
            {
                if (NodeEnumOverlay.IsActive(property))
                {
                    NodeEnumOverlay.Close();
                }
                else
                {
                    XNode.Node node = property.serializedObject.targetObject as XNode.Node;
                    float zoom = NodeEditorWindow.current.zoom;

                    Vector2 nodeWindowPos = NodeEditorWindow.current.GridToWindowPosition(node.position);
                    Vector2 menuTopLeft = nodeWindowPos + new Vector2(buttonRect.x, buttonRect.yMax) / zoom;
                    float physicalWidth = buttonRect.width / zoom;

                    NodeEnumOverlay.Open(property, menuTopLeft, physicalWidth, NodeEditorWindow.current);
                }
                Event.current.Use();
            }

            if (NodeEnumOverlay.IsActive(property))
            {
                NodeEditorWindow.current.onLateGUI -= NodeEnumOverlay.DoGUI;
                NodeEditorWindow.current.onLateGUI += NodeEnumOverlay.DoGUI;
            }

            EditorGUI.EndProperty();
        }
    }

    public static class NodeEnumOverlay
    {
        // --- ADJUSTABLE SETTINGS ---
        private static float cornerRadius = 4f; // Adjust this for more/less roundness

        private static SerializedObject so;
        private static string propPath;
        private static string[] names;
        private static int selectedIdx;
        private static int hoverIdx = -1;

        private static NodeEditorWindow hostWindow;
        private static Vector2 targetPos;
        private static float targetWidth;
        private static Rect menuRect;

        private static float initialZoom;
        private static Vector2 initialPan;
        private static Vector2 initialWindowSize;
        private static double startTime;
        private static Vector2 scrollPos;
        private static bool isActive;

        private static Color bgColor = new Color(0.18f, 0.18f, 0.18f, 0.96f);
        private static Color frameColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        private static Color hoverTint = new Color(0.353f, 0.380f, 0.412f, 1f);
        private static Color selectTint = new Color(0.25f, 0.25f, 0.25f, 1f);
        private static Color textActive = Color.white;

        private static GUIStyle labelStyle, bgStyle, borderStyle, topStyle, botStyle, midStyle, singleStyle;
        private const float itemHeight = 22f;

        public static bool IsActive(SerializedProperty prop)
        {
            if (!isActive || so == null || so.targetObject == null) return false;
            try { return propPath == prop.propertyPath && so.targetObject == prop.serializedObject.targetObject; }
            catch { return false; }
        }

        public static void Open(SerializedProperty prop, Vector2 pos, float width, NodeEditorWindow window)
        {
            if (window == null) return;

            so = prop.serializedObject;
            propPath = prop.propertyPath;
            names = prop.enumDisplayNames;
            selectedIdx = prop.enumValueIndex;
            hostWindow = window;
            targetPos = pos;
            targetWidth = width;

            initialZoom = hostWindow.zoom;
            initialPan = hostWindow.panOffset;
            initialWindowSize = hostWindow.position.size;

            startTime = EditorApplication.timeSinceStartup;
            scrollPos = Vector2.zero;
            isActive = true;

            NodeEditorWindow.onProcessModalEvents = HandleEvent;
        }

        public static void Close()
        {
            isActive = false;
            NodeEditorWindow.onProcessModalEvents = null;
            if (hostWindow != null) hostWindow.onLateGUI -= DoGUI;
            so = null;
            if (hostWindow != null) hostWindow.Repaint();
            hostWindow = null;
        }

        public static bool HandleEvent(Event e)
        {
            if (!isActive || hostWindow == null)
            {
                Close();
                return false;
            }

            if (e.type == EventType.ScrollWheel)
            {
                Close();
                return false;
            }

            float width = Mathf.Max(targetWidth, 120f);
            float height = Mathf.Min(names.Length * itemHeight + 2f, 400f);
            menuRect = new Rect(targetPos.x, targetPos.y + 2f, width, height);

            if (menuRect.Contains(e.mousePosition))
            {
                EditorGUIUtility.AddCursorRect(menuRect, MouseCursor.Arrow);

                Vector2 localMouse = e.mousePosition - menuRect.position;
                float contentY = localMouse.y + scrollPos.y - 1f;
                int newHover = Mathf.FloorToInt(contentY / itemHeight);

                if (newHover >= 0 && newHover < names.Length) hoverIdx = newHover;
                else hoverIdx = -1;

                if (e.type == EventType.MouseDown && e.button == 0 && hoverIdx != -1)
                {
                    try
                    {
                        SerializedProperty prop = so.FindProperty(propPath);
                        if (prop != null)
                        {
                            prop.enumValueIndex = hoverIdx;
                            so.ApplyModifiedProperties();

                            GUIUtility.keyboardControl = 0;
                            GUIUtility.hotControl = 0;
                            EditorGUIUtility.editingTextField = false;
                            GUI.FocusControl(null);

                            if (so.targetObject != null) EditorUtility.SetDirty(so.targetObject);
                            so.Update();
                        }
                    }
                    catch { }

                    e.Use();
                    Close();
                    return true;
                }

                if (e.type == EventType.MouseMove || e.type == EventType.MouseDown)
                {
                    e.Use();
                    return true;
                }
            }
            else if (e.type == EventType.MouseDown)
            {
                Close();
            }

            return false;
        }

        public static void DoGUI()
        {
            if (!isActive || hostWindow == null || names == null)
            {
                isActive = false;
                return;
            }

            if (Mathf.Abs(hostWindow.zoom - initialZoom) > 0.005f ||
                (hostWindow.panOffset - initialPan).sqrMagnitude > 1.0f ||
                hostWindow.position.size != initialWindowSize)
            {
                Close();
                return;
            }

            if (Event.current.type == EventType.Repaint)
            {
                if (bgStyle == null || bgStyle.normal.background == null ||
                    borderStyle == null || borderStyle.normal.background == null)
                {
                    InitStyles();
                }
            }

            if (bgStyle == null || bgStyle.normal.background == null) return;

            float alpha = Mathf.Clamp01((float)(EditorApplication.timeSinceStartup - startTime) / 0.10f);

            Color oldC = GUI.color;
            GUI.color = new Color(bgColor.r, bgColor.g, bgColor.b, alpha * bgColor.a);
            GUI.Box(menuRect, "", bgStyle);
            GUI.color = new Color(frameColor.r, frameColor.g, frameColor.b, alpha);
            GUI.Box(menuRect, "", borderStyle);
            GUI.color = oldC;

            Rect scrollRect = new Rect(menuRect.x + 1, menuRect.y + 1, menuRect.width - 2, menuRect.height - 2);

            GUI.BeginGroup(scrollRect);
            for (int i = 0; i < names.Length; i++)
            {
                Rect itemRect = new Rect(0, i * itemHeight - scrollPos.y, menuRect.width - 2, itemHeight);
                bool isHover = (i == hoverIdx);
                bool isSelected = (i == selectedIdx);

                if (isHover || isSelected)
                {
                    GUIStyle hStyle = (names.Length == 1) ? singleStyle : (i == 0 ? topStyle : (i == names.Length - 1 ? botStyle : midStyle));
                    GUI.color = isHover ? new Color(hoverTint.r, hoverTint.g, hoverTint.b, alpha) : new Color(selectTint.r, selectTint.g, selectTint.b, alpha);
                    GUI.Box(itemRect, "", hStyle);
                    GUI.color = oldC;
                }

                labelStyle.normal.textColor = (isHover || isSelected) ? new Color(1, 1, 1, alpha) : new Color(0.75f, 0.75f, 0.75f, alpha);
                GUI.Label(new Rect(itemRect.x + 8, itemRect.y, itemRect.width - 8, itemRect.height), names[i], labelStyle);
            }
            GUI.EndGroup();

            if (alpha < 1f) hostWindow.Repaint();
        }

        private static void InitStyles()
        {
            labelStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 12, alignment = TextAnchor.MiddleLeft };

            // Background uses the main radius
            bgStyle = CreateTex(true, true, cornerRadius, false);
            borderStyle = CreateTex(true, true, cornerRadius, true);

            // Highlight items use a slightly smaller radius to fit perfectly inside the container
            float itemRadius = Mathf.Max(0, cornerRadius - 1f);
            singleStyle = CreateTex(true, true, itemRadius, false);
            topStyle = CreateTex(true, false, itemRadius, false);
            botStyle = CreateTex(false, true, itemRadius, false);

            midStyle = new GUIStyle { normal = { background = Texture2D.whiteTexture } };
        }

        private static GUIStyle CreateTex(bool top, bool bot, float r, bool outline)
        {
            int res = 32; Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dx = Mathf.Max(0, r - x, x - (res - 1 - r));
                    float dy = (top && y > res / 2) ? Mathf.Max(0, y - (res - 1 - r)) : (bot && y <= res / 2 ? Mathf.Max(0, r - y) : 0);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(r - dist + 0.5f);
                    if (outline) a = Mathf.Clamp01(a - Mathf.Clamp01((r - 1f) - dist + 0.5f));
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            }
            tex.Apply();
            return new GUIStyle
            {
                normal = { background = tex },
                border = new RectOffset((int)r + 1, (int)r + 1, (int)r + 1, (int)r + 1)
            };
        }
    }
}
