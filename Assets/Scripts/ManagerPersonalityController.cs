using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.TiledDungeon;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledDungeon.Enemies;
using UnityEngine;

public class ManagerPersonalityController : MonoBehaviour
{
    private void Start()
    {
        if (WWSettings.ManagerPersonality.Value != ManagerPersonality.Zealous)
        {
            DisableManager();
        }

        Debug.Log($"Manager Personality: {WWSettings.ManagerPersonality.Value}");
    }

    private void OnEnable()
    {
        WWSettings.ManagerPersonality.OnChange += ManagerPersonality_OnChange;
    }

    private void OnDisable()
    {
        WWSettings.ManagerPersonality.OnChange -= ManagerPersonality_OnChange;
    }

    private void ManagerPersonality_OnChange(ManagerPersonality value)
    {
        if (value == ManagerPersonality.Golfer)
        {
            DisableManager();
        } else if (value == ManagerPersonality.Zealous) {
            if (!Attentive)
            {
                var enemy = EnableManager();
                enemy.ReEnterActiveState();
            }
        }
    }

    public bool Attentive { get; private set; } = true;

    void DisableManager()
    {
        Attentive = false;

        // Enemy is already paused from being in settings

        var entity = GetComponent<GridEntity>();
        var smoothMovement = GetComponent<SmoothMovementTransitions>();

        if (smoothMovement != null)
        {
            smoothMovement.Halt();
            foreach (var node in smoothMovement.Reservations)
            {
                node.RemoveReservation(entity);
            }
        }

        entity.Sync();
        entity.Moving = MovementType.Stationary;
        entity.enabled = true;

        GetComponent<TDDangerZone>().enabled = false;
        GetComponentInChildren<TDEnemyPerception>(true).enabled = false;

        transform.HideAllChildren();

        Debug.Log("Manager Personality: not attentive");
    }

    public TDEnemy EnableManager()
    {
        Attentive = true;

        var enemy = GetComponent<TDEnemy>();
        if (enemy.Paused)
        {
            // By default we should not unpause enemy because we might be in settings
            enemy.ReEnterActiveState();
        }

        var entity = GetComponent<GridEntity>();
        entity.enabled = true;

        GetComponent<TDDangerZone>().enabled = true;
        GetComponentInChildren<TDEnemyPerception>(true).enabled = true;

        transform.ShowAllChildren();

        Debug.Log("Manager Personality: attentive");
        return enemy;
    }

    public void RestoreEnemyAt(TDNode node, Direction lookDirection, TDPathCheckpoint checkpoint)
    {
        if (WWSettings.ManagerPersonality.Value == ManagerPersonality.Zealous)
        {
            // Otherwise we don't spawn in correctly
            DisableManager();
        }

        var enemy = EnableManager();
        enemy.Entity.Node = node;

        enemy.Entity.Sync();
        enemy.ForceActivity(LMCore.EntitySM.State.StateType.Patrolling, lookDirection);
        var patrolling = enemy.ActivePatrolling;
        if (patrolling == null)
        {
            Debug.LogWarning("Manager Personality: there's no active patrolling behaviour");
            return;
        }

        patrolling.ForceSetCheckpoint(checkpoint, 1);
        enemy.Paused = false;
    }
}
