using UnityEngine;
using UnityEditor;

public class DialogueSceneGraphSpawner
{
    // This adds a button to the right-click menu in the Hierarchy window!
    [MenuItem("GameObject/Dialogue/Dialogue Scene Graph", false, 10)]
    public static void CreateDialogueSceneGraph(MenuCommand menuCommand)
    {
        // 1. Create the new GameObject and name it automatically
        GameObject go = new GameObject("DialogueSceneGraph");

        // 2. Attach the xNode SceneGraph component to it
        go.AddComponent<DialogueSceneGraph>();

        // 3. Parent it to whatever the user right-clicked on (if anything)
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

        // 4. Register this action so you can use CTRL+Z to undo it
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);

        // 5. Automatically select the new object in the hierarchy
        Selection.activeObject = go;
    }
}