using UnityEngine;
using System.Collections.Generic;

public abstract class BaseScenario : MonoBehaviour
{
    public enum ResetMode { World, List }
    public abstract GameStage Stage { get; }

    [Header("Configuration")]
     public ResetMode resetMode = ResetMode.World;
    public bool isCheckpoint;


    [HideInInspector]
    public List<Resettable> manualResetList = new List<Resettable>();

    public virtual void OnEnter()
    {
        ResetObjects();

        if (isCheckpoint)
        {
            ScenarioManager.Instance.SetCheckpoint(Stage);
        }

    }

    public virtual void OnExit()
    {
    }

    private void ResetObjects()
    {
        if (resetMode == ResetMode.World)
        {
            var resettables = FindObjectsByType<Resettable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (Resettable resettable in resettables)
            {
                resettable.ResetState();
            }
        }
        else
        {
            foreach (Resettable resettable in manualResetList)
            {
                if (resettable != null)
                {
                    resettable.ResetState();
                }
            }
        }
    }
}