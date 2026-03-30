using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(AudioPlayer))]
public class AudioPlayerEditor : Editor
{
    private GUIStyle upperCenterLabelStyle;
    private GUIStyle middleRightLabelStyle;
    private int selectedKeyIndex = -1;
    private int draggingKeyIndex = -1;
    private int draggingTangentType = -1;

    private Rect _currentGraphRect;
    private float _xMaxView = 1.05f;
    private float _yMaxView = 1.15f;

    public override void OnInspectorGUI()
    {
        // Theme Colors
        Color marginBackgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        Color marginBorderColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        Color graphBackgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        Color graphBorderColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        Color curveLineColor = new Color(1f, 0.2f, 0.2f, 1f);
        Color majorGridColor = new Color(0f, 0f, 0f, 0.2f);
        Color minorGridColor = new Color(0f, 0f, 0f, 0.08f);

        serializedObject.Update();
        AudioPlayer player = (AudioPlayer)target;

        if (player.rolloffMode != AudioRolloffMode.Custom) selectedKeyIndex = -1;

        // Start listening for changes in the inspector
        EditorGUI.BeginChangeCheck();

        // --- DYNAMIC PROPERTY ITERATOR ---
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (prop.name == "m_Script") continue;

            if (prop.name == "minDistance")
            {
                if (player.rolloffMode == AudioRolloffMode.Custom)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField(prop.displayName, "Controlled by the curve.");
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUILayout.PropertyField(prop, true);
                }
                continue;
            }

            if (prop.name == "rolloffCurve")
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField(prop.displayName, EditorStyles.boldLabel);

                if (player.rolloffMode == AudioRolloffMode.Custom)
                {
                    EditorGUILayout.HelpBox("CUSTOM MODE: Double-Click to add points. Right-Click to delete. Select a point to edit tangents.", MessageType.Info);
                }

                DrawGraphUI(player, marginBackgroundColor, marginBorderColor, graphBackgroundColor, graphBorderColor, curveLineColor, majorGridColor, minorGridColor);
                continue;
            }

            EditorGUILayout.PropertyField(prop, true);
        }

        // If any properties changed in the inspector, apply them and regenerate the curves
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            ValidateAndGenerateCurves(player);
            EditorUtility.SetDirty(player); // Flag the object to be saved
        }
        else
        {
            serializedObject.ApplyModifiedProperties();
        }
    }

    // --- NEW: Validation and Curve Generation Logic Moved Here ---
    private void ValidateAndGenerateCurves(AudioPlayer player)
    {
        Undo.RecordObject(player, "Update Audio Curves");

        if (player.rolloffMode != player.lastRolloffMode)
        {
            switch (player.rolloffMode)
            {
                case AudioRolloffMode.Custom: GenerateCustomCurve(player); break;
                case AudioRolloffMode.Logarithmic: GenerateLogarithmicCurve(player); break;
                case AudioRolloffMode.Linear: GenerateLinearCurve(player); break;
            }
            player.lastRolloffMode = player.rolloffMode;
        }

        if (player.rolloffMode == AudioRolloffMode.Custom) player.minDistance = 0f;
        else if (player.minDistance < 0.001f) player.minDistance = 0.001f;

        if (player.maxDistance <= player.minDistance) player.maxDistance = player.minDistance + 0.1f;

        if (player.rolloffMode != AudioRolloffMode.Custom)
        {
            if (player.rolloffMode == AudioRolloffMode.Logarithmic) GenerateLogarithmicCurve(player);
            else GenerateLinearCurve(player);
        }
    }

    private void GenerateCustomCurve(AudioPlayer player)
    {
        player.rolloffCurve.keys = new Keyframe[0];
        player.rolloffCurve.AddKey(new Keyframe(0f, 1f));
        player.rolloffCurve.AddKey(new Keyframe(1f, 0f));
    }

    private void GenerateLinearCurve(AudioPlayer player)
    {
        player.rolloffCurve.keys = new Keyframe[0];
        float normMin = player.minDistance / player.maxDistance;
        float slope = -1f / (1f - normMin);
        player.rolloffCurve.AddKey(new Keyframe(normMin, 1f, 0f, slope));
        player.rolloffCurve.AddKey(new Keyframe(1f, 0f, slope, 0f));
    }

    private void GenerateLogarithmicCurve(AudioPlayer player)
    {
        player.rolloffCurve.keys = new Keyframe[0];
        float safeMin = Mathf.Max(player.minDistance, 0.001f);
        float ratio = player.maxDistance / safeMin;
        int steps = Mathf.CeilToInt(Mathf.Log(ratio, 2f));
        float multiplier = Mathf.Pow(ratio, 1f / steps);

        for (int i = 0; i <= steps; i++)
        {
            float dist = safeMin * Mathf.Pow(multiplier, i);
            if (i == steps) dist = player.maxDistance;

            float vol = safeMin / dist;
            float t = dist / player.maxDistance;

            float slope = -safeMin / (t * t * player.maxDistance);

            player.rolloffCurve.AddKey(new Keyframe(t, vol, slope, slope));
        }
    }

    // --- EXISTING GRAPH UI LOGIC (Unchanged, just uses 'player' correctly) ---
    private void DrawGraphUI(AudioPlayer player, Color marginBG, Color marginBorder, Color graphBG, Color graphBorder, Color curveColor, Color majorGrid, Color minorGrid)
    {
        Rect fullRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(170));
        EditorGUI.DrawRect(fullRect, marginBG);
        Handles.color = marginBorder;
        Handles.DrawWireCube(fullRect.center, fullRect.size);

        float marginLeft = 35f, marginBottom = 20f, marginTop = 15f, marginRight = 15f;
        _currentGraphRect = new Rect(fullRect.x + marginLeft, fullRect.y + marginTop, fullRect.width - marginLeft - marginRight, fullRect.height - marginTop - marginBottom);

        Rect interactionRect = new Rect(_currentGraphRect.x - 20, _currentGraphRect.y - 20, _currentGraphRect.width + 40, _currentGraphRect.height + 40);

        _xMaxView = 1.05f;
        _yMaxView = 1.15f;

        EditorGUI.DrawRect(_currentGraphRect, graphBG);
        Handles.color = graphBorder;
        Handles.DrawWireCube(_currentGraphRect.center, _currentGraphRect.size);

        HandleEvents(player, interactionRect);

        Handles.BeginGUI();
        DrawGrid(player, majorGrid, minorGrid, marginLeft, fullRect);
        DrawCurveAndPoints(player, curveColor);
        Handles.EndGUI();
    }

    private void HandleEvents(AudioPlayer player, Rect interactionRect)
    {
        Event e = Event.current;
        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        if (player.rolloffMode != AudioRolloffMode.Custom) return;

        if (e.type == EventType.MouseDown && interactionRect.Contains(e.mousePosition))
        {
            Undo.RecordObject(player, "Edit Curve");
            if (e.button == 0)
            {
                float bestDist = 15f;
                int bestIndex = -1;

                if (selectedKeyIndex >= 0)
                {
                    Keyframe k = player.rolloffCurve[selectedKeyIndex];
                    Vector2 l = ToPx(k.time - 0.08f, k.value - (k.inTangent * 0.08f));
                    Vector2 r = ToPx(k.time + 0.08f, k.value + (k.outTangent * 0.08f));
                    if (Vector2.Distance(e.mousePosition, l) < 15f) { draggingTangentType = 0; bestIndex = selectedKeyIndex; }
                    else if (Vector2.Distance(e.mousePosition, r) < 15f) { draggingTangentType = 1; bestIndex = selectedKeyIndex; }
                }

                if (draggingTangentType == -1)
                {
                    for (int i = 0; i < player.rolloffCurve.length; i++)
                    {
                        float d = Vector2.Distance(e.mousePosition, ToPx(player.rolloffCurve[i].time, player.rolloffCurve[i].value));
                        if (d < bestDist) { bestDist = d; bestIndex = i; }
                    }
                    if (bestIndex != -1) { selectedKeyIndex = draggingKeyIndex = bestIndex; }
                }

                if (bestIndex == -1 && e.clickCount == 2)
                {
                    Vector2 v = ToVal(e.mousePosition);
                    player.rolloffCurve.AddKey(Mathf.Clamp01(v.x), Mathf.Clamp01(v.y));
                }
                if (bestIndex != -1) GUIUtility.hotControl = controlID;
                else selectedKeyIndex = -1;
                e.Use();
            }
            else if (e.button == 1)
            {
                for (int i = 0; i < player.rolloffCurve.length; i++)
                {
                    if (Vector2.Distance(e.mousePosition, ToPx(player.rolloffCurve[i].time, player.rolloffCurve[i].value)) < 15f)
                    {
                        player.rolloffCurve.RemoveKey(i); selectedKeyIndex = -1; break;
                    }
                }
                e.Use();
            }
        }
        if (e.type == EventType.MouseDrag && GUIUtility.hotControl == controlID)
        {
            Vector2 v = ToVal(e.mousePosition);
            if (draggingKeyIndex >= 0)
            {
                Keyframe k = player.rolloffCurve[draggingKeyIndex];
                float minT = (draggingKeyIndex > 0) ? player.rolloffCurve[draggingKeyIndex - 1].time + 0.001f : 0f;
                float maxT = (draggingKeyIndex < player.rolloffCurve.length - 1) ? player.rolloffCurve[draggingKeyIndex + 1].time - 0.001f : 1.0f;
                k.time = Mathf.Clamp(v.x, minT, maxT);
                k.value = Mathf.Clamp01(v.y);
                player.rolloffCurve.MoveKey(draggingKeyIndex, k);
            }
            else if (draggingTangentType >= 0)
            {
                Keyframe k = player.rolloffCurve[selectedKeyIndex];
                float dt = v.x - k.time;
                if (Mathf.Abs(dt) > 0.001f) k.inTangent = k.outTangent = (v.y - k.value) / dt;
                player.rolloffCurve.MoveKey(selectedKeyIndex, k);
            }
            GUI.changed = true; e.Use();
        }
        if (e.type == EventType.MouseUp) { GUIUtility.hotControl = 0; draggingKeyIndex = -1; draggingTangentType = -1; }
    }

    private void DrawGrid(AudioPlayer player, Color majorGrid, Color minorGrid, float marginLeft, Rect fullRect)
    {
        // Cache styles to prevent memory leaks
        if (upperCenterLabelStyle == null)
            upperCenterLabelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperCenter };
        if (middleRightLabelStyle == null)
            middleRightLabelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };

        float roughStep = player.maxDistance / 5f;
        float mag = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(Mathf.Max(roughStep, 0.01f))));
        float norm = roughStep / mag;
        float majorStep = (norm < 1.5f ? 1 : norm < 3.5f ? 2 : norm < 7.5f ? 5 : 10) * mag;

        for (float val = 0; val <= player.maxDistance * 1.1f; val += majorStep / 5f)
        {
            float t = val / player.maxDistance;
            bool isMajor = (val % majorStep) < (majorStep * 0.1f) || (val % majorStep) > (majorStep * 0.9f);
            float px = _currentGraphRect.x + (t / _xMaxView) * _currentGraphRect.width;
            if (px > _currentGraphRect.xMax) continue;
            Handles.color = isMajor ? majorGrid : minorGrid;
            Handles.DrawLine(new Vector2(px, _currentGraphRect.y), new Vector2(px, _currentGraphRect.yMax));

            // USE CACHED STYLE HERE
            if (isMajor) GUI.Label(new Rect(px - 25, _currentGraphRect.yMax + 2, 50, 20), val.ToString("F1"), upperCenterLabelStyle);
        }

        for (float y = 0; y <= 1.01f; y += 0.5f)
        {
            float py = _currentGraphRect.yMax - (y / _yMaxView) * _currentGraphRect.height;
            Handles.color = majorGrid;
            Handles.DrawLine(new Vector2(_currentGraphRect.x, py), new Vector2(_currentGraphRect.xMax, py));

            GUI.Label(new Rect(fullRect.x, py - 10, marginLeft - 5, 20), y.ToString("F1"), middleRightLabelStyle);
        }
    }

    private void DrawCurveAndPoints(AudioPlayer player, Color curveColor)
    {
        if (player.rolloffCurve.length > 0)
        {
            Handles.color = curveColor;
            List<Vector3> points = new List<Vector3>();
            points.Add(ToPx(0f, GetEval(player, 0f)));
            AddAdaptivePoints(player, 0f, 1f, 0.003f, points, 0);
            Vector3 lastPoint = ToPx(1f, GetEval(player, 1f));
            if (Vector2.Distance(points[points.Count - 1], lastPoint) > 0.1f) points.Add(lastPoint);
            Handles.DrawAAPolyLine(3f, points.ToArray());
        }

        for (int i = 0; i < player.rolloffCurve.length; i++)
        {
            Keyframe k = player.rolloffCurve[i];
            float drawT = (player.rolloffMode == AudioRolloffMode.Custom) ? k.time : k.time / player.maxDistance;
            if (drawT > 1.0f) continue;
            Vector2 p = ToPx(k.time, k.value);

            if (i == selectedKeyIndex && player.rolloffMode == AudioRolloffMode.Custom)
            {
                Vector2 lp = ToPx(k.time - 0.08f, k.value - (k.inTangent * 0.08f));
                Vector2 r = ToPx(k.time + 0.08f, k.value + (k.outTangent * 0.08f));
                Handles.color = Color.yellow;
                Handles.DrawLine(p, lp); Handles.DrawLine(p, r);
                Handles.DrawSolidDisc(lp, Vector3.forward, 3f); Handles.DrawSolidDisc(r, Vector3.forward, 3f);
            }
            Handles.color = (i == selectedKeyIndex) ? Color.yellow : Color.white;
            Handles.DrawSolidDisc(p, Vector3.forward, 4f);
            Handles.color = new Color(0.1f, 0.1f, 0.1f);
            Handles.DrawSolidDisc(p, Vector3.forward, 2f);
        }
    }

    private void AddAdaptivePoints(AudioPlayer player, float tStart, float tEnd, float threshold, List<Vector3> points, int depth)
    {
        if (depth > 12) return;
        float tMid = (tStart + tEnd) * 0.5f;
        float vStart = GetEval(player, tStart);
        float vEnd = GetEval(player, tEnd);
        float vMid = GetEval(player, tMid);
        float linearMid = (vStart + vEnd) * 0.5f;
        bool needsSubdivision = Mathf.Abs(vMid - linearMid) > threshold;
        if (depth < 5 && tStart < 0.1f) needsSubdivision = true;
        if (needsSubdivision)
        {
            AddAdaptivePoints(player, tStart, tMid, threshold, points, depth + 1);
            AddAdaptivePoints(player, tMid, tEnd, threshold, points, depth + 1);
        }
        else points.Add(ToPx(tEnd, vEnd));
    }

    private float GetEval(AudioPlayer player, float t)
    {
        return player.rolloffCurve.Evaluate(t);
    }

    private Vector2 ToPx(float t, float v) => new Vector2(_currentGraphRect.x + (t / _xMaxView) * _currentGraphRect.width, _currentGraphRect.yMax - (v / _yMaxView) * _currentGraphRect.height);
    private Vector2 ToVal(Vector2 px) => new Vector2(((px.x - _currentGraphRect.x) / _currentGraphRect.width) * _xMaxView, ((_currentGraphRect.yMax - px.y) / _currentGraphRect.height) * _yMaxView);

    // --- NEW: Gizmo drawing moved out of runtime using Unity's built-in attribute ---
    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    static void DrawGizmoForAudioPlayer(AudioPlayer player, GizmoType gizmoType)
    {
        Gizmos.DrawIcon(player.transform.position, "AudioSource Gizmo", true);

        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.5f);
        Gizmos.DrawWireSphere(player.transform.position, player.minDistance);
        
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.2f);
        Gizmos.DrawWireSphere(player.transform.position, player.maxDistance);
    }
}

