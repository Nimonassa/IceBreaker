using UnityEngine;

public class StrugglingInWaterScenario : BaseScenario
{
    [Header("Water Extras")]
    public GameObject splashParticles;

    public override GameStage Stage => GameStage.StrugglingInWater;
    public override void OnEnter()
    {
        base.OnEnter();

        if (splashParticles != null)
        {
            splashParticles.SetActive(true);
        }
    }

    public override void OnExit()
    {
        base.OnExit();
    }
}