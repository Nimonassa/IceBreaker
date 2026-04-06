using UnityEngine;
using System.Collections.Generic;

public class ScenarioManager : MonoBehaviour
{
    public static ScenarioManager Instance { get; private set; }

    public GameStage CurrentStage => currentStage;

    [Header("Debug Status")]
    [SerializeField, HideInInspector] private GameStage currentStage;
    [SerializeField, HideInInspector] private GameStage lastCheckpoint;


    private Dictionary<GameStage, BaseScenario> scenarios = new Dictionary<GameStage, BaseScenario>();
    private BaseScenario activeScenario;

    private void Awake()
    {
        Instance = this;
        BaseScenario[] foundScenarios = FindObjectsByType<BaseScenario>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (BaseScenario scenario in foundScenarios)
        {
            if (scenarios.ContainsKey(scenario.Stage))
            {
                Debug.LogError($"You have two scenarios trying to be the {scenario.Stage} stage!");
                continue;
            }

            scenarios.Add(scenario.Stage, scenario);
        }

    }

    public void SetCheckpoint(GameStage checkpoint)
    {
        lastCheckpoint = checkpoint;
    }

    public void ChangeStage(GameStage nextStage)
    {
        if (!scenarios.TryGetValue(nextStage, out BaseScenario target))
        {
            Debug.LogError($"Stage {nextStage} not found in the scene! Did you forget to create the GameObject?");
            return;
        }

        if (activeScenario != null)
        {
            activeScenario.OnExit();
        }

        currentStage = nextStage;
        activeScenario = target;
        activeScenario.OnEnter();
    }

    public void FailToCheckpoint()
    {
        ChangeStage(lastCheckpoint);
    }
}