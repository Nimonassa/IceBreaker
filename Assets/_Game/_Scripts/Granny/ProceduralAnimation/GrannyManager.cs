using UnityEngine;

public class GrannyManager : MonoBehaviour
{
    [field: SerializeField] public RockingChair rockingChair { get; private set; }
    [field: SerializeField] public GrannyLegSwing leftLeg { get; private set; }
    [field: SerializeField] public GrannyLegSwing rightLeg { get; private set; }
    [field: SerializeField] public GrannyLookAt lookAt { get; private set; }

    private void Awake() => InitializeComponents();
    private void Reset() => InitializeComponents();

    [ContextMenu("Refresh Components")]
    public void InitializeComponents()
    {
        rockingChair ??= GetComponentInChildren<RockingChair>();
        lookAt ??= GetComponentInChildren<GrannyLookAt>();

        foreach (var swinger in GetComponentsInChildren<GrannyLegSwing>())
        {
            if (swinger.side == GrannyLegSwing.LegSide.Left) 
                leftLeg = swinger;
            else 
                rightLeg = swinger;
        }
    }
}
