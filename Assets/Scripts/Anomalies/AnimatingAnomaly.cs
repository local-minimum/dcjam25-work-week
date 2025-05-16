using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.TiledDungeon;
using LMCore.TiledDungeon.DungeonFeatures;
using System.Collections.Generic;
using UnityEngine;

public class AnimatingAnomaly : AbsAnomaly
{
    [SerializeField]
    GameObject anomalyRoot;

    [SerializeField]
    Animator animator;

    [SerializeField]
    string startTrigger;

    [SerializeField]
    bool triggerWhenEnterArea;

    [SerializeField]
    int retriggerAreaSize = 2;

    [SerializeField]
    string disableSiblingByName;

    [HelpBox("These objects gets disabled in anomaly state and enabled in normal state")]
    [SerializeField]
    List<GameObject> disabledObjects = new List<GameObject>();

    [HelpBox("These objects gets enabled in anomaly state and disabled in normal state")]
    [SerializeField]
    List<GameObject> enabledObjects = new List<GameObject>();

    [SerializeField, Header("Horror")]
    TDDecoration managerSpawn;

    [SerializeField]
    TDDecoration managerTarget;

    [SerializeField]
    AudioSource speaker;

    protected override void OnDisableExtra()
    {
        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
        LevelRegion.OnEnterRegion -= LevelRegion_OnEnterRegion;
        if (speaker != null) speaker.Stop();

    }

    protected override void OnEnableExtra()
    {
        LevelRegion.OnEnterRegion += LevelRegion_OnEnterRegion;

        if (speaker != null)
        {
            if (SpawnedManager)
            {
                speaker.Play();
            } else
            {
                speaker.Stop();
            }
        }

        foreach (var obj in enabledObjects)
        {
            obj.SetActive(false);
        }
    }

    bool SpawnedManager
    {
        get
        {
            var manager = Dungeon.GetEntity("Manager", includeDisabled: true);
            if (manager.enabled)
            {
                var personality = manager.GetComponent<ManagerPersonalityController>();
                if (personality != null)
                {
                    return personality.ManagerIsRestored;
                }
            }
            return false;
        }
    }

    private void LevelRegion_OnEnterRegion(GridEntity entity, string regionId)
    {
        if (SpawnedManager || 
            managerSpawn == null || 
            managerTarget == null || 
            entity.EntityType != GridEntityType.PlayerCharacter || 
            anomalyActive == false) return;

        var myRegion = GetComponentInParent<LevelRegion>();
        if (myRegion == null || myRegion.RegionId != regionId) return;


        if (WWSettings.ManagerPersonality.Value != ManagerPersonality.Golfer)
        {
            var manager = Dungeon.GetEntity("Manager", includeDisabled: true);
            if (manager != null)
            {
                var controller = manager.GetComponent<ManagerPersonalityController>();
                if (controller != null)
                {
                    controller.RestoreEnemyAt(
                        managerSpawn.GetComponentInParent<TDNode>(),
                        Direction.East,
                        managerTarget.GetComponentInParent<TDPathCheckpoint>());
                }
            }
        }

        if (speaker != null)
        {
            speaker.Play();
        }
    }

    bool anomalyActive;

    protected override void SetAnomalyState()
    {
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;

        if (anomalyRoot != null)
        {
            anomalyRoot.SetActive(true);
        }
        if (animator != null && !triggerWhenEnterArea)
        {
            Debug.Log($"Anomaly '{anomalyId}' triggers '{startTrigger}' on {animator}");
            animator.SetTrigger(startTrigger);
        }
       
        if (animator != null)
        {
            animator.enabled = true;
        }

        ToggleSiblingByName(false);

        foreach (var obj in disabledObjects)
        {
            obj.SetActive(false);
        }

        foreach (var obj in enabledObjects)
        {
            obj.SetActive(true);
        }

        anomalyActive = true;
    }

    bool wasInTriggerArea;

    private void GridEntity_OnPositionTransition(GridEntity entity)
    {
        if (!triggerWhenEnterArea || entity.EntityType != GridEntityType.PlayerCharacter) return;

        bool inTriggerArea = Coordinates.ManhattanDistance(entity.Coordinates) <= retriggerAreaSize;

        if (inTriggerArea && !wasInTriggerArea)
        {
            if (animator != null)
            {
                Debug.Log($"Anomaly '{anomalyId}' area re-triggers '{startTrigger}' on {animator}");
                animator.SetTrigger(startTrigger);
            }
        }

        wasInTriggerArea = inTriggerArea;
    }

    void ToggleSiblingByName(bool setActive)
    {
        if (!string.IsNullOrEmpty(disableSiblingByName))
        {
            var parent = transform.parent;
            for (int i = 0, n = parent.childCount; i<n;i++)
            {
                var sibing = parent.GetChild(i);
                if (sibing == transform) continue;
                if (sibing.name == disableSiblingByName)
                {
                    sibing.gameObject.SetActive(setActive);
                    break;
                }
            }
        }

        foreach (var obj in disabledObjects)
        {
            obj.SetActive(true);
        }
    }

    protected override void SetNormalState()
    {
        anomalyActive = false;
        if (anomalyRoot != null)
        {
            anomalyRoot.SetActive(false);
        }
        ToggleSiblingByName(true);

        if (speaker != null)
        {
            speaker.Stop();
        }

        foreach (var obj in disabledObjects)
        {
            obj.SetActive(true);
        }

        foreach (var obj in enabledObjects)
        {
            obj.SetActive(false);
        }
       
        if (animator != null)
        {
            animator.enabled = false;
        }

        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
    }
}
