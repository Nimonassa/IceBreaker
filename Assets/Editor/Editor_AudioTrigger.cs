using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioTrigger))]
public class AudioTriggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Find the playOn property
        SerializedProperty playOnProp = serializedObject.FindProperty("playOn");

        // Check conditions
        AudioTrigger.TriggerType triggerType = (AudioTrigger.TriggerType)playOnProp.intValue;
        
        bool isPhysicsTrigger = triggerType == AudioTrigger.TriggerType.OnTriggerEnter ||
                                triggerType == AudioTrigger.TriggerType.OnTriggerExit ||
                                triggerType == AudioTrigger.TriggerType.OnCollisionEnter;

        // Only show Exit Action if we are ENTERING a physics trigger
        bool showExitAction = triggerType == AudioTrigger.TriggerType.OnTriggerEnter || 
                              triggerType == AudioTrigger.TriggerType.OnCollisionEnter;

        // Start looping through all variables
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;

            // 1. Skip drawing the Tag and Layer fields entirely if it's not a physics trigger
            if (!isPhysicsTrigger && (iterator.name == "filterTag" || iterator.name == "filterLayer"))
            {
                continue;
            }

            // 2. Skip drawing the Exit Action if we didn't select an "Enter" trigger type
            if (!showExitAction && iterator.name == "onExit")
            {
                continue;
            }

            // 3. Draw the fields
            using (new EditorGUI.DisabledScope(iterator.name == "m_Script"))
            {
                if (iterator.name == "filterTag")
                {
                    // Manually recreate the Header and some spacing since TagField ignores attributes
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);

                    // Draw the built-in Unity Tag Dropdown
                    iterator.stringValue = EditorGUILayout.TagField(new GUIContent("Tag"), iterator.stringValue);
                }
                else if (iterator.name == "filterLayer")
                {
                    // Give the layer a clean label
                    EditorGUILayout.PropertyField(iterator, new GUIContent("Layer"), true);
                }
                else
                {
                    // Draw everything else normally (which respects their [Header] attributes)
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }


    // =========================================================================
    // GIZMOS
    // =========================================================================

    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    static void DrawGizmoForAudioTrigger(AudioTrigger trigger, GizmoType gizmoType)
    {
        // Only draw if there is a valid linked AudioPlayer
        if (trigger.audioPlayer != null)
        {
            Vector3 playerPos = trigger.audioPlayer.transform.position;

            // 1. Draw the official Unity AudioSource icon at the emitter's location
            // (The 'true' parameter allows the icon to scale as you zoom in and out)
            Gizmos.DrawIcon(playerPos, "AudioSource Gizmo", true);

            // 2. Draw the Min and Max distance spheres
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.5f);
            Gizmos.DrawWireSphere(playerPos, trigger.audioPlayer.minDistance);

            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.2f);
            Gizmos.DrawWireSphere(playerPos, trigger.audioPlayer.maxDistance);

            // 3. Draw a faint connecting line so you know WHICH player this trigger targets
            if (trigger.transform.position != playerPos)
            {
                Handles.color = new Color(1f, 1f, 1f, 0.3f);
                Handles.DrawDottedLine(trigger.transform.position, playerPos, 4f);
            }
        }
    }
}