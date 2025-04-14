using LMCore.Crawler;
using LMCore.TiledDungeon;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledImporter;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DoorAreaCustomOpeners : TDFeature, ITDCustom
{
    [SerializeField]
    bool OnlyPlayer = true;

    [SerializeField, HideInInspector]
    int areaId;

    static Dictionary<int, bool> WasHere = new Dictionary<int, bool>();
    static Dictionary<int, List<TDDoor>> Doors = new Dictionary<int, List<TDDoor>>();
    static Dictionary<int, List<DoorAreaCustomOpeners>> Areas = new Dictionary<int, List<DoorAreaCustomOpeners>>();

    public void Configure(TDNode node, TiledCustomProperties properties)
    {
        areaId = properties.Int("AreaId");
    }

    private void OnEnable()
    {
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
    }

    private void OnDisable()
    {
        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
    }

    private void Start()
    {
        var doors = Node.GetComponentsInChildren<TDDoor>();
        if (!Doors.ContainsKey(areaId))
        {
            Doors.Add(areaId, doors.ToList());
        } else
        {
            Doors[areaId].AddRange(doors);
        }

        if (!Areas.ContainsKey(areaId))
        {
            Areas.Add(areaId, new List<DoorAreaCustomOpeners>() { this });
        } else
        {
            Areas[areaId].Add(this);
        }
    }

    private void OnDestroy()
    {
        if (Doors.ContainsKey(areaId))
        {
            Doors[areaId].Clear();
        }
        Areas[areaId].Remove(this);
    }

    bool wasHere => 
        WasHere.ContainsKey(areaId) ? WasHere[areaId] : false;

    bool IsInArea(Vector3Int coordinates)
    {
        if (Areas.ContainsKey(areaId))
        {
            foreach (var part in Areas[areaId])
            {
                if (part.Coordinates == coordinates) return true;
            }
        }
        return false;
    }

    private void GridEntity_OnPositionTransition(GridEntity entity)
    {
        if (OnlyPlayer && entity.EntityType != GridEntityType.PlayerCharacter) return;

        var wasHere = this.wasHere;
        var isHere = IsInArea(entity.Coordinates);

        var doors = Doors.ContainsKey(areaId) ? Doors[areaId] : null;
        if (doors == null)
        {
            Debug.LogError($"Custom area door opener {name} has no door in it");
            return;
        }

        if (!wasHere && isHere)
        {
            WasHere[areaId] = true;
            foreach (var door in doors)
            {
                door.OpenDoor(entity);
            }
            Debug.Log($"Custom area door opener {name}: Opening doors");
        } else if (wasHere && !isHere)
        {
            WasHere[areaId] = false;
            foreach (var door in doors)
            {
                door.CloseDoor(entity);
            }
            Debug.Log($"Custom area door opener {name}: Closing doors");
        }
    }

    [ContextMenu("Info")]
    void Info()
    {
        Debug.Log($"Custom area door opener {name} / {areaId}: {Doors.Count} doors, was here: {wasHere}");
    }
}
