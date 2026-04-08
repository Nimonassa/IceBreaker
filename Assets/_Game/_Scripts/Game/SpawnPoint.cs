using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpawnPoint : MonoBehaviour
{
    private void Reset()
    {
        this.gameObject.name = "Spawn Point";
#if UNITY_EDITOR
        Texture2D icon = EditorGUIUtility.IconContent("sv_label_6").image as Texture2D;
        EditorGUIUtility.SetIconForObject(this.gameObject, icon);
#endif
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawRay(transform.position, transform.forward * 1.0f);
    }
}