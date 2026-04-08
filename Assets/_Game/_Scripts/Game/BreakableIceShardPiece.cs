using UnityEngine;

[DisallowMultipleComponent]
public class BreakableIceShardPiece : MonoBehaviour
{
    [Tooltip("Generated 2D center of this shard in local ice-space, used for break selection.")]
    [SerializeField] private Vector2 localCenter;
    [Tooltip("Selection padding around this shard so nearby impacts can include it in the break.")]
    [SerializeField] private float influenceRadius = 0.1f;
    [Tooltip("Captured local position used to restore the shard before replaying the break animation.")]
    [SerializeField] private Vector3 restLocalPosition;
    [Tooltip("Captured local rotation, stored as Euler angles, used to restore the shard pose.")]
    [SerializeField] private Vector3 restLocalEulerAngles;
    [Tooltip("Captured local scale used to restore the shard to its original size.")]
    [SerializeField] private Vector3 restLocalScale = Vector3.one;

    public Vector2 LocalCenter => localCenter;
    public float InfluenceRadius => influenceRadius;

    public void Configure(Vector2 shardLocalCenter, float shardInfluenceRadius)
    {
        localCenter = shardLocalCenter;
        influenceRadius = shardInfluenceRadius;
        CaptureRestPose();
    }

    public void CaptureRestPose()
    {
        restLocalPosition = transform.localPosition;
        restLocalEulerAngles = transform.localEulerAngles;
        restLocalScale = transform.localScale;
    }

    public void RestoreRestPose()
    {
        transform.localPosition = restLocalPosition;
        transform.localRotation = Quaternion.Euler(restLocalEulerAngles);
        transform.localScale = restLocalScale;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (restLocalScale == Vector3.zero)
            restLocalScale = transform.localScale;
    }
#endif
}
