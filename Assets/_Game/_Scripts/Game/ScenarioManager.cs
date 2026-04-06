using UnityEngine;
using System.Collections.Generic;

public class ScenarioManager : MonoBehaviour
{
    public static ScenarioManager Instance { get; private set; }

    public GameStage CurrentStage => currentStage;

    [Header("Configuration")]
    [SerializeField] private GameStage defaultStage = GameStage.None;

    [Header("Debug Status")]
    [SerializeField, HideInInspector] private GameStage currentStage = GameStage.None;
    [SerializeField, HideInInspector] private GameStage lastCheckpoint = GameStage.None;

    private Dictionary<GameStage, BaseScenario> scenarios = new Dictionary<GameStage, BaseScenario>();
    private BaseScenario activeScenario;

    private void Awake()
    {
        Instance = this;
        BaseScenario[] foundScenarios = FindObjectsByType<BaseScenario>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (BaseScenario scenario in foundScenarios)
        {
            if (scenario.Stage == GameStage.None)
            {
                continue;
            }

            if (scenarios.ContainsKey(scenario.Stage))
            {
                Debug.LogError($"You have two scenarios trying to be the {scenario.Stage} stage!");
                continue;
            }

            scenarios.Add(scenario.Stage, scenario);
        }
    }

    private void Start()
    {
        if (defaultStage != GameStage.None)
        {
            ChangeStage(defaultStage);
        }
    }

    public void SetCheckpoint(GameStage checkpoint)
    {
        if (checkpoint != GameStage.None)
        {
            lastCheckpoint = checkpoint;
        }
    }

    public void ChangeStage(GameStage nextStage)
    {
        if (nextStage == GameStage.None)
        {
            return;
        }

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
        if (lastCheckpoint != GameStage.None)
        {
            ChangeStage(lastCheckpoint);
        }
    }
}
