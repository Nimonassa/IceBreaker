using UnityEngine;

public class GrannyArmAnchor : MonoBehaviour
{
    public enum ArmSide { Left, Right }

    [Header("Tagging")] public ArmSide side;
    [Header("Arm References")] public Transform upperArm, forearm, upperPivot, forePivot;
    [Header("Settings"), Range(0f, 1f)] public float anchorStrength = 1f;
    [Header("Lock Options")] public bool lockUpperPosition, lockForePosition;

    void Start() => CaptureInitialPose();

    public void CaptureInitialPose()
    {
        if (upperArm && upperPivot) upperPivot.SetPositionAndRotation(upperArm.position, upperArm.rotation);
        if (forearm && forePivot) forePivot.SetPositionAndRotation(forearm.position, forearm.rotation);
    }

    void LateUpdate()
    {
        SolveBone(upperArm, upperPivot, lockUpperPosition);
        SolveBone(forearm, forePivot, lockForePosition);
    }

    private void SolveBone(Transform bone, Transform pivot, bool lockPos)
    {
        if (!bone || !pivot || !bone.parent) return;

        bone.localRotation = Quaternion.Slerp(bone.localRotation, Quaternion.Inverse(bone.parent.rotation) * pivot.rotation, anchorStrength);

        if (lockPos)
            bone.localPosition = Vector3.Lerp(bone.localPosition, bone.parent.InverseTransformPoint(pivot.position), anchorStrength);
    }
}
