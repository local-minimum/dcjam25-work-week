using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.IO;
using LMCore.TiledDungeon;
using LMCore.TiledDungeon.Enemies;
using System.Linq;
using UnityEngine;

public class BossBattleTrigger : AbsAnomaly, IOnLoadSave
{
    [SerializeField]
    LayerMask LOSFilter;

    [SerializeField]
    float maxDistance = 10f;

    bool ManagerGroggy
    {
        get
        {
            var manager = Manager;
            if (manager != null)
            {
                var criteria = manager.GetComponentInChildren<CountDownCriteria>(true);
                return criteria != null && criteria.enabled;
            }

            return false;
        }
    }

    GridEntity Player => Dungeon.Player;
    GridEntity Manager => Dungeon.GetEntity("Manager");

    protected override void OnEnableExtra()
    {
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
        BossBattleManager.OnGroggyManger += SetManagerGroggyPatrolling;
    }

    protected override void OnDisableExtra()
    {
        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
        BossBattleManager.OnGroggyManger -= SetManagerGroggyPatrolling;
    }


    bool FallbackManagerDanger()
    {
        var player = this.Player;
        var manager = this.Manager;

        var distance = player.Coordinates.ChebyshevDistance(manager.Coordinates);

        if (distance > 1) return false;
        else if (distance == 0) return true;

        var sources = manager
            .GetComponentsInChildren<TDEnemyPerception>()
            .Where(p => p.RequireLOS)
            .Select(p => p.RayCaster.position)
            .ToList();

        sources.Add(manager.LookTarget.position);


        var target = player.LookTarget.position;

        return sources.Any(s => CheckLOS(s, target));
    }

    private bool CheckLOS(Vector3 source, Vector3 target)
    {
        Vector3 direction = target - source;

        if (Physics.Raycast(source, direction, out var hitInfo, maxDistance, LOSFilter))
        {
            if (hitInfo.transform.GetComponentInParent<GridEntity>() == Player)
            {
                return true;
            }
            Debug.Log($"BBTrigger: LOS hit {hitInfo.transform.name}, not {Player.name}");
        }
        return false;
    }

    private void GridEntity_OnPositionTransition(GridEntity entity)
    {
        if (!ManagerGroggy && (TDDangerZone.In(Player) || FallbackManagerDanger()))
        {
            var personality = Manager.GetComponent<ManagerPersonalityController>();

            if (personality == null || personality.Attentive)
            {
                EnterBossBattle();
            }
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

    void DisableManager()
    {
        var enemy = Manager.GetComponent<TDEnemy>();
        enemy.ForceActivity(LMCore.EntitySM.State.StateType.Loitering);
        enemy.Paused = true;
        // Pause normally pauses this too
        var anim = enemy.GetComponentInChildren<Animator>(true);
        anim.enabled = true;
        anim.SetTrigger("Guard");
        Manager.GetComponent<TDDangerZone>().enabled = false;
    }

    void SetManagerGroggyPatrolling()
    {
        Manager.transform.rotation = managerStartLook;
        var enemy = Manager.GetComponent<TDEnemy>();
        enemy.Paused = false;
        // Use count down criteria instead
        var criteria = enemy.GetComponentInChildren<CountDownCriteria>(true);
        if (criteria != null)
        {
            criteria.Restore();
            Debug.Log("BBTrigger: Patroling duration critera restored");
        }
        enemy.ForceActivity(LMCore.EntitySM.State.StateType.Patrolling);
        // Manager.GetComponent<TDDangerZone>().enabled = false;
        Debug.Log($"BBTrigger: Forcing {enemy.name} to patrol");
    }

    void DisablePlayer()
    {
        Player.MovementBlockers.Add(this);
        var freeLookCamera = Player.GetComponentInChildren<FreeLookCamera>(true);
        if (freeLookCamera != null)
        {
            freeLookCamera.enabled = false;
        }
    }

    /*
    void RestorePlayer()
    {
        Player.transform.rotation = playerStartLook;
        Player.MovementBlockers.Remove(this);
        var freeLookCamera = Player.GetComponentInChildren<FreeLookCamera>(true);
        if (freeLookCamera != null)
        {
            freeLookCamera.enabled = true;
        }
    }
    */

    float lookStart;
    bool lookEasing;

    void EnterBossBattle()
    {
        Debug.Log($"BBTrigger: Start Conflict with {Manager.name}, groggy {ManagerGroggy}!");
        DisablePlayer();

        if (Manager != null)
        {
            DisableManager();

            Vector3 managerToPlayer = Vector3.zero;

            var man2play = Manager.transform.position - Player.transform.position;
            playerStartLook = Player.transform.rotation;
            playerGoalLook = Quaternion.LookRotation(man2play, Vector3.up);
            managerStartLook = Manager.transform.rotation;
            managerGoalLook = Quaternion.LookRotation(-man2play, Vector3.up);
        }

        lookStart = Time.timeSinceLevelLoad;
        lookEasing = Manager != null;
        Debug.Log($"BBTrigger: Need easing {lookEasing} (Manager is {Manager})");

        Player.MovementBlockers.Add(this);

        // We're just gonna be  dead soon if anomaly no need to save that
        if (!anomalousBoss)
        {
            BossBattleManager.SafeInstance.SetBattleStartedAndSave();
        }

        if (!lookEasing) LoadManagerFightMiniGame();
    }

    void LoadManagerFightMiniGame()
    {
        lookEasing = false;

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

            Player.transform.rotation = Quaternion.Lerp(playerStartLook, playerGoalLook, progress);
            Manager.transform.rotation = Quaternion.Lerp(managerStartLook, managerGoalLook, progress);

            if (progress == 1f)
            {
                Debug.Log($"BBTrigger: Look easing done, trigger boss fight");
                LoadManagerFightMiniGame();
            }
        }
    }

    // Must be after BossBattleManager
    // Must be before PlayerEntity or we reenter battle
    // TODO: Maybe must do stuff to enemy so might need to be after
    public int OnLoadPriority => 10;

    public void OnLoad<T>(T save) where T : new()
    {
        if (ManagerGroggy)
        {
            SetManagerGroggyPatrolling();
        }
    }

    [ContextMenu("Info")]
    void Info()
    {
        Debug.Log($"BBTrigger: Groggy {ManagerGroggy}, InZone {TDDangerZone.In(Player)}, Fallback {FallbackManagerDanger()}");
    }

}
