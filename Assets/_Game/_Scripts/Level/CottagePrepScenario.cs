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


    private bool dialogueFinished = false;
    private bool checklistFinished = false;

    public override async void OnEnter()
    {
        base.OnEnter();
        checklist.Hide();
        questMarker.Show();
        
        part1Trigger.gameObject.SetActive(true);
        part4Trigger.gameObject.SetActive(false);

        await Part1();
        await Part2();
        await Part3();
        await Part4();

        SceneManager.LoadScene(1);
    }

    // Part 1 - Speaking to granny
    private async Task Part1()
    {
        dialogueFinished = false;
        Action OnStarted = () => { PlayerManager.Instance.Load(noMovementConfig); };
        Action OnCompleted = () => { PlayerManager.Instance.Load(defaultConfig); dialogueFinished = true; };

        DialogueManager.Instance.LoadDialogue(part1Dialogue.node, OnStarted, OnCompleted);

        while (!dialogueFinished) { await Task.Yield(); }
    }

    // Part 2 - Collecting items
    private async Task Part2()
    {
        part1Trigger.gameObject.SetActive(false);

        checklistFinished = false;
        checklist.Show();
        checklist.OnCompleted.AddListener(() => checklistFinished = true);

        while (!checklistFinished) { await Task.Yield(); }
    }

    // Part 3 - Collecting items feedback
    private async Task Part3()
    {

        dialogueFinished = false;
        checklist.OnCompleted.RemoveAllListeners();
        DialogueManager.Instance.LoadDialogue(part3Dialogue.node, null, () => dialogueFinished = true);
        DialogueManager.Instance.PlayDialogue();

        while (!dialogueFinished) { await Task.Yield(); }
    }

    // Part 4 - Before leaving safety tips
    private async Task Part4()
    {
        part4Trigger.gameObject.SetActive(true);

        dialogueFinished = false;
        DialogueManager.Instance.LoadDialogue(part4Dialogue.node, null, () => dialogueFinished = true);

        while (!dialogueFinished) { await Task.Yield(); }
    }
    
}