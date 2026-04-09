using UnityEngine;

public class RotateTowards : MonoBehaviour
{
    public Transform target;
    
    [Header("Used Axes")]
    public bool orientPitch = true; 
    public bool orientYaw = true;   
    public bool orientRoll = false; 

    [Header("Invert Axes")]
    public bool invertPitch = false;
    public bool invertYaw = false;
    public bool invertRoll = false;

    void LateUpdate()
    {

        if (target == null) return;

        Vector3 directionToTarget = target.position - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(-directionToTarget);
        
        Vector3 targetEuler = targetRotation.eulerAngles;
        Vector3 currentEuler = transform.eulerAngles;

        float pitchOffset = invertPitch ? 180f : 0f;
        float yawOffset   = invertYaw   ? 180f : 0f;
        float rollOffset  = invertRoll  ? 180f : 0f;

        float finalPitch = orientPitch ? targetEuler.x + pitchOffset : currentEuler.x;
        float finalYaw   = orientYaw   ? targetEuler.y + yawOffset   : currentEuler.y;
        float finalRoll  = orientRoll  ? targetEuler.z + rollOffset  : currentEuler.z;

        transform.rotation = Quaternion.Euler(finalPitch, finalYaw, finalRoll);
    }
}