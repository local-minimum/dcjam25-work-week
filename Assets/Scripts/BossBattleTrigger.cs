using LMCore.Crawler;
using LMCore.TiledDungeon;
using LMCore.TiledDungeon.Enemies;
using UnityEngine;

public class BossBattleTrigger : AbsAnomaly
{
    protected override void OnDisableExtra()
    {
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
    }

    protected override void OnEnableExtra()
    {
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
    }

    protected override void SetAnomalyState()
    {
        // TODO: If we get to do monster manager!
    }

    protected override void SetNormalState()
    {
    }

    GridEntity player;
    GridEntity manager;

    private void GridEntity_OnPositionTransition(GridEntity entity)
    {

        if (player == null && entity.EntityType == GridEntityType.PlayerCharacter)
        {
            player = entity;
        }

        if (managerGroggySteps > 0)
        {
            if (entity.EntityType == GridEntityType.PlayerCharacter)
            {
                managerGroggySteps--;
                if (managerGroggySteps == 0)
                {
                    RestoreManger();
                }
            }
            return;
        }

        if (player == null) return;

        if (TDDangerZone.In(player))
        {
            EnterBossBattle();
        }
    }

    void DisableManager()
    {
        manager.GetComponent<TDEnemy>().ForceActivity(LMCore.EntitySM.State.StateType.Loitering);
        manager.MovementBlockers.Add(this);
        manager.GetComponent<TDDangerZone>().enabled = false;
    }

    void RestoreManger()
    {
        manager.GetComponent<TDEnemy>().ForceActivity(LMCore.EntitySM.State.StateType.Guarding);
        manager.MovementBlockers.Remove(this);
        manager.GetComponent<TDDangerZone>().enabled = false;
    }

    void DisablePlayer()
    {
        player.MovementBlockers.Add(this);
        var freeLookCamera = player.GetComponentInChildren<FreeLookCamera>(true);
        if (freeLookCamera != null)
        {
            freeLookCamera.enabled = false;
        }
    }

    void RestorePlayer()
    {
        player.MovementBlockers.Remove(this);
        var freeLookCamera = player.GetComponentInChildren<FreeLookCamera>(true);
        if (freeLookCamera != null)
        {
            freeLookCamera.enabled = true;
        }
    }

    float lookStart;
    bool lookEasing;

    void EnterBossBattle()
    {
        DisablePlayer();

        manager = player.Dungeon.GetEntity("Manager");

        if (manager != null)
        {
            DisableManager();

            Vector3 managerToPlayer = Vector3.zero;

            var man2play = manager.transform.position - player.transform.position;
            playerStartLook = player.transform.rotation;
            playerGoalLook = Quaternion.LookRotation(man2play, Vector3.up);
            managerStartLook = manager.transform.rotation;
            managerGoalLook = Quaternion.LookRotation(-man2play, Vector3.up);
        }

        lookStart = Time.timeSinceLevelLoad;
        lookEasing = manager != null;
    }

    void TriggerBossGame()
    {

    }

    public void FailMiniGame()
    {
        AnomalyManager.instance.FailBossBattle();
    }

    [SerializeField]
    int managerGroggyAfterLossSteps = 10;

    int managerGroggySteps;
    public void WinMiniGame()
    {
        // TODO: Sync up look directions

        RestorePlayer();
        managerGroggySteps = managerGroggyAfterLossSteps;
    }



    [SerializeField]
    float lookDuration = 0.5f;
    Quaternion playerStartLook;
    Quaternion playerGoalLook;
    Quaternion managerStartLook;
    Quaternion managerGoalLook;

    private void Update()
    {
        if (lookEasing)
        {
            var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - lookStart) / lookDuration);

            player.transform.rotation = Quaternion.Lerp(playerStartLook, playerGoalLook, progress);
            manager.transform.rotation = Quaternion.Lerp(managerStartLook, managerGoalLook, progress);
            lookEasing = false;

            TriggerBossGame();
        }
    }
}
