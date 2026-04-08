using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class ClimbingOutScenario : BaseScenario
{
    [Header("Climbing Extras")]
    public GameObject iceAwls;

    public override GameStage Stage => GameStage.ClimbingOut;

    public override void OnEnter()
    {
        base.OnEnter();

        if (iceAwls != null)
        {
            iceAwls.SetActive(true);
        }
    }

    public override void OnExit()
    {
        base.OnExit();

    }
}