using UnityEngine;

public class CottagePrepScenario : BaseScenario
{
    public override GameStage Stage => GameStage.CottagePreparation;

    [Header("Dialogue References")]
    public DialogueReference introDialogue;
    public DialogueReference readyToExitDialogue;
    
    public void Start()
    {
        if (introDialogue.IsValid)
        {
            introDialogue.Play(onComplete:() => Debug.Log("Hello World!"));
        }
    }

    public override void OnEnter()
    {
        base.OnEnter();

        if (introDialogue.IsValid)
        {
            introDialogue.Play(onComplete: () => Debug.Log("Hello World!"));
        }
    }

}