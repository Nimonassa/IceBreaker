using UnityEngine;
using System.Collections.Generic;

public abstract class BaseScenario : MonoBehaviour
{
    public enum ResetMode { World, List }
    public abstract GameStage Stage { get; }

    [Header("Configuration")]
    public PlayerConfig config;
    public SpawnPoint spawnPoint;
    public bool isCheckpoint;

    [Header("Reset Settings")]
    public ResetMode resetMode = ResetMode.World;
    
    
    [HideInInspector]
    public List<Resettable> manualResetList = new List<Resettable>();

    public virtual void OnEnter()
    {
        ResetObjects();

        if (isCheckpoint)
        {
            ScenarioManager.Instance.SetCheckpoint(Stage);
        }

        if (spawnPoint != null)
        {
            PlayerManager player = PlayerManager.Instance;
            
            if (player != null)
            {
                player.Teleport(spawnPoint.transform);
            }
        }

        if (config != null)
        {
            PlayerManager player = PlayerManager.Instance;
            
            if (player != null)
            {
                player.Load(config);
            }
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