using UnityEngine;

public class IceBreakScenario : BaseScenario
{
    [Header("Ice Break Extras")]
    public AudioSource crackSound;
    public GameObject solidIce;
    public GameObject brokenIce;

    public override GameStage Stage => GameStage.IceBreak;

    public override void OnEnter()
    {
        base.OnEnter();

        if (crackSound != null)
        {
            crackSound.Play();
        }

        if (solidIce != null)
        {
            solidIce.SetActive(false);
        }

        if (brokenIce != null)
        {
            brokenIce.SetActive(true);
        }
    }
}