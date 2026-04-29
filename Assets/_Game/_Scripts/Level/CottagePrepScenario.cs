using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CottagePrepScenario : BaseScenario
{
    public override GameStage Stage => GameStage.CottagePreparation;

    
    [Header("Dialogue References")]
    public DialogueReference part1Dialogue;
    public DialogueReference part3Dialogue;
    public DialogueReference part4Dialogue;

    [Header("Dialogue Triggers")]
    public InteractionTrigger part1Trigger;
    public InteractionTrigger part4Trigger;

    [Header("Components")]
    public NotebookChecklist checklist;
    public QuestMarker questMarker;

    [Header("Player Configs")]
    public PlayerConfig noMovementConfig;
    public PlayerConfig defaultConfig;




    public override async void OnEnter()
    {
        base.OnEnter();
        checklist.Hide();
        questMarker.Show();
        
        part1Trigger.gameObject.SetActive(true);
        part4Trigger.gameObject.SetActive(false);

        await Part1(); // Granny dialogue
        await Part2(); // Items collection
        await Part3(); // Collection feedback
        await Part4(); // Safety tips
 
        SceneManager.LoadScene(1);
    }

    // Part 1 - Speaking to granny
    private async Task Part1()
    {
        var completed = new TaskCompletionSource<bool>(false);

        Action OnStarted = () => { PlayerManager.Instance.Load(noMovementConfig); };
        Action OnCompleted = () => { PlayerManager.Instance.Load(defaultConfig); completed.SetResult(true); };

        DialogueManager.Instance.LoadDialogue(part1Dialogue.node, OnStarted, OnCompleted);

        await completed.Task;
    }

    // Part 2 - Collecting items
    private async Task Part2()
    {
        var completed = new TaskCompletionSource<bool>(false);

        part1Trigger.gameObject.SetActive(false);
        checklist.Show();
        checklist.OnCompleted.AddListener(() => completed.SetResult(true));

        await completed.Task;
    }

    // Part 3 - Collecting items feedback
    private async Task Part3()
    {
        var completed = new TaskCompletionSource<bool>(false);

        checklist.OnCompleted.RemoveAllListeners();
        DialogueManager.Instance.LoadDialogue(part3Dialogue.node, null, () => completed.SetResult(true));
        DialogueManager.Instance.PlayDialogue();

        await completed.Task;
    }

    // Part 4 - Before leaving safety tips
    private async Task Part4()
    {
        var completed = new TaskCompletionSource<bool>(false);

        part4Trigger.gameObject.SetActive(true);
        DialogueManager.Instance.LoadDialogue(part4Dialogue.node, null, () => completed.SetResult(true));

        await completed.Task;
    }
    
}