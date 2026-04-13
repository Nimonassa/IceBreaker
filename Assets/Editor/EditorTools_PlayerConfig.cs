using UnityEngine;
using UnityEditor;

public static class PlayerConfigEditorTools
{
    // This adds a right-click option directly to the PlayerConfig ScriptableObject
    [MenuItem("CONTEXT/PlayerConfig/Pull Settings From Scene")]
    public static void PullFromScene(MenuCommand command)
    {
        // Get the specific ScriptableObject we just right-clicked
        PlayerConfig config = (PlayerConfig)command.context;

        // Find the player manager currently active in the scene
        PlayerMovement player = Object.FindObjectOfType<PlayerMovement>();
        if (player == null)
        {
            Debug.LogWarning("PlayerConfig: Could not find a PlayerMovement component in the active scene.");
            return;
        }

        // We use SerializedObjects to read the private fields from PlayerMovement
        SerializedObject playerSO = new SerializedObject(player);
        SerializedObject configSO = new SerializedObject(config);

        configSO.Update();

        // --- Move Settings ---
        configSO.FindProperty("moveMode").enumValueIndex = playerSO.FindProperty("moveMode").enumValueIndex;
        configSO.FindProperty("moveHand").enumValueIndex = playerSO.FindProperty("moveHand").enumValueIndex;
        configSO.FindProperty("moveSpeed").floatValue = playerSO.FindProperty("moveSpeed").floatValue;
        configSO.FindProperty("teleportDistance").floatValue = playerSO.FindProperty("teleportDistance").floatValue;
        configSO.FindProperty("shiftDistance").floatValue = playerSO.FindProperty("shiftDistance").floatValue;
        configSO.FindProperty("shiftSpeed").floatValue = playerSO.FindProperty("shiftSpeed").floatValue;

        // --- Turn Settings ---
        configSO.FindProperty("turnMode").enumValueIndex = playerSO.FindProperty("turnMode").enumValueIndex;
        configSO.FindProperty("turnHand").enumValueIndex = playerSO.FindProperty("turnHand").enumValueIndex;
        configSO.FindProperty("continuousTurnSpeed").floatValue = playerSO.FindProperty("continuousTurnSpeed").floatValue;
        configSO.FindProperty("snapTurnAmount").floatValue = playerSO.FindProperty("snapTurnAmount").floatValue;
        configSO.FindProperty("shiftTurnAmount").floatValue = playerSO.FindProperty("shiftTurnAmount").floatValue;
        configSO.FindProperty("shiftTurnSpeed").floatValue = playerSO.FindProperty("shiftTurnSpeed").floatValue;

        // Apply changes and force Unity to save the asset
        configSO.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        Debug.Log($"<b>Success!</b> Pulled settings from <i>{player.gameObject.name}</i> into <i>{config.name}</i>.", config);
    }
}
