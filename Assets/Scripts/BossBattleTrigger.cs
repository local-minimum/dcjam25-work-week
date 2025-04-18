using LMCore.Crawler;
using LMCore.IO;
using LMCore.TiledDungeon;
using LMCore.TiledDungeon.Enemies;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BossBattleTrigger : AbsAnomaly, IOnLoadSave
{
    bool managerGroggy;

    protected override void OnEnableExtra()
    {
        player = Dungeon.Player;
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
        BossBattleManager.OnMangerNotGroggy += BossBattleManager_OnMangerNotGroggy;
    }


    protected override void OnDisableExtra()
    {
        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
        BossBattleManager.OnMangerNotGroggy -= BossBattleManager_OnMangerNotGroggy;
    }

    private void BossBattleManager_OnMangerNotGroggy()
    {
        managerGroggy = false;
        RestoreManger();
    }

    private void GridEntity_OnPositionTransition(GridEntity entity)
    {
        if (player == null)
        {
            player = Dungeon.Player;
        }

        if (!managerGroggy && TDDangerZone.In(player))
        {
            EnterBossBattle();
        }
    }


    bool anomalousBoss;

    protected override void SetAnomalyState()
    {
        anomalousBoss = true;
    }

    protected override void SetNormalState()
    {
        anomalousBoss = false;
    }

    GridEntity player;
    GridEntity manager;

    void DisableManager()
    {
        if (manager == null)
        {
            manager = Dungeon.GetEntity("Manager");
        }
        var enemy = manager.GetComponent<TDEnemy>();
        enemy.ForceActivity(LMCore.EntitySM.State.StateType.Loitering);
        enemy.Paused = true;
        // Pause normally pauses this too
        var anim = enemy.GetComponentInChildren<Animator>(true);
        anim.enabled = true;
        anim.SetTrigger("Guard");
        manager.GetComponent<TDDangerZone>().enabled = false;
    }

    void RestoreManger()
    {
        manager.transform.rotation = managerStartLook;
        var enemy = manager.GetComponent<TDEnemy>();
        enemy.Paused = false;
        enemy.ForceActivity(LMCore.EntitySM.State.StateType.Guarding);
        manager.GetComponent<TDDangerZone>().enabled = true;
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
        player.transform.rotation = playerStartLook;
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
        Debug.Log("BBTrigger: Start Conflict!");
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

        // We're just gonna be  dead soon if anomaly no need to save that
        if (!anomalousBoss)
        {
            BossBattleManager.SafeInstance.SetBattleStartedAndSave();
        }

        if (!lookEasing) TriggerBossGame();
    }

    void TriggerBossGame()
    {
        if (anomalousBoss)
        {
            Debug.Log("BBTrigger: We're entering anomaly death");
            TriggerAnomaly();
        } else
        {
            Debug.Log("BBTrigger: We're entering boss battle");
            BossBattleManager.SafeInstance.LoadBossFight();
        }
    }

    public void TriggerAnomaly()
    {
        AnomalyManager.instance.DeathByAnomaly();
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

    // Must be after BossBattleManager
    // Must be before PlayerEntity or we reenter battle
    // TODO: Maybe must do stuff to enemy so might need to be after
    public int OnLoadPriority => 10;

    public void OnLoad<T>(T save) where T : new()
    {
        managerGroggy = BossBattleManager.SafeInstance.GroggyBoss;

        if (managerGroggy)
        {
            DisableManager();
        }
    }
}
