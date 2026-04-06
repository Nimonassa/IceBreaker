using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(BoxCollider))] 
public class StageTrigger : MonoBehaviour
{
    public GameStage stageToTrigger;

    private void Reset()
    {
        
        this.gameObject.name = "Stage Trigger";
        BoxCollider col = this.GetComponent<BoxCollider>();
        if (col != null)
            col.isTrigger = true;
        
        #if UNITY_EDITOR
        Texture2D icon = EditorGUIUtility.IconContent("sv_label_3").image as Texture2D;
        EditorGUIUtility.SetIconForObject(this.gameObject, icon);
        #endif
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TriggerStage();
        }
    }

    public void TriggerStage()
    {
        if (ScenarioManager.Instance.CurrentStage != stageToTrigger)
        {
            ScenarioManager.Instance.ChangeStage(stageToTrigger);
        }
    }
}