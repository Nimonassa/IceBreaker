using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


[RequireComponent(typeof(BoxCollider))]
public class FailTrigger : MonoBehaviour
{
    private void Reset()
    {
        this.gameObject.name = "Fail Trigger";

        BoxCollider col = this.GetComponent<BoxCollider>();
        if (col != null)
            col.isTrigger = true;
        #if UNITY_EDITOR
        Texture2D icon = EditorGUIUtility.IconContent("sv_label_6").image as Texture2D;
        EditorGUIUtility.SetIconForObject(this.gameObject, icon);
        #endif
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TriggerFail();
        }
    }

    public void TriggerFail()
    {
        ScenarioManager.Instance.FailToCheckpoint();
    }
}