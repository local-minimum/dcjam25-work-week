using LMCore.Crawler;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChickenWorkersAnomaly : AbsAnomaly
{
    [SerializeField]
    List<ChickenWorkerAnomalyTarget> ChickenWorkers = new List<ChickenWorkerAnomalyTarget>();

    [SerializeField]
    List<ChickenWorkerAnomalyTarget> Chairs = new List<ChickenWorkerAnomalyTarget>();

    [SerializeField]
    List<Vector3Int> ZoneOffsets = new List<Vector3Int>();

    protected override void OnEnableExtra()
    {
        foreach (var chicken in ChickenWorkers)
        {
            chicken.gameObject.SetActive(!chicken.VisibleThroughWindow);
        }
    }

    protected override void OnDisableExtra()
    {
        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
    }

    protected override void SetAnomalyState()
    {
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
    }

    protected override void SetNormalState()
    {
        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
    }

    bool wasInsideTriggerZone;

    private void GridEntity_OnPositionTransition(GridEntity entity)
    {
        if (entity.EntityType != GridEntityType.PlayerCharacter) return;

        var inside = ZoneOffsets.Any(off => Coordinates + off == entity.Coordinates);

        if (inside == wasInsideTriggerZone) return;

        foreach (var chicken in ChickenWorkers)
        {
            chicken.gameObject.SetActive(chicken.VisibleThroughWindow == inside);
        }

        foreach (var chair in Chairs)
        {
            chair.gameObject.SetActive(chair.VisibleThroughWindow == inside);
        }

        wasInsideTriggerZone = inside;
    }
}
