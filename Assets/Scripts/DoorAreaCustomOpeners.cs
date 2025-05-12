using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.TiledDungeon;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledImporter;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DoorAreaCustomOpeners : TDFeature, ITDCustom
{
    [HelpBox("These values should only be set in the prefab or in Tiled.", HelpBoxMessageType.Warning)]
    [SerializeField, Tooltip("Default value if not 'OpenForPlayer' or 'IngorePlayer' has been set in Tiled")]
    bool openForPlayer = true;

    [SerializeField, Tooltip("Default value if not 'OpenForEnemy' or 'IngoreEnemy' has been set in Tiled")]
    bool openForEnemy = false;

    [SerializeField, HideInInspector]
    int areaId;

    static Dictionary<int, List<GridEntity>> WasHere = new Dictionary<int, List<GridEntity>>();
    static Dictionary<int, List<TDDoor>> Doors = new Dictionary<int, List<TDDoor>>();
    static Dictionary<int, List<DoorAreaCustomOpeners>> Areas = new Dictionary<int, List<DoorAreaCustomOpeners>>();

    public void Configure(TDNode node, TiledCustomProperties properties)
    {
        areaId = properties.Int("AreaId");
        openForPlayer = properties.Bool("OpenForPlayer", !properties.Bool("IgnorePlayer", !openForPlayer));
        openForEnemy = properties.Bool("OpenForEnemy", !properties.Bool("IgnoreEnemy", !openForEnemy));
        Info();
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
        } else if (Doors[areaId].Count == 0)
        {
            Doors[areaId].AddRange(doors);
        } else
        {
            foreach (var door in doors)
            {
                if (Doors[areaId].Contains(door)) continue;

                Doors[areaId].Add(door);
            }
        }

        if (!Areas.ContainsKey(areaId))
        {
            Areas.Add(areaId, new List<DoorAreaCustomOpeners>() { this });
        }
        else if (!Areas[areaId].Contains(this))
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

    bool HasOccupants => 
        WasHere.ContainsKey(areaId) ? (WasHere[areaId]?.Count ?? 0) > 0 : false;

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

    bool RelevantEntityType(GridEntity entity)
    {
        switch (entity.EntityType) {
            case GridEntityType.PlayerCharacter:
                return openForPlayer;
            case GridEntityType.Enemy:
                return openForEnemy;
            default:
                return false;
        }
    }

    private void GridEntity_OnPositionTransition(GridEntity entity)
    {

        if (!RelevantEntityType(entity)) return;

        var isHere = IsInArea(entity.Coordinates);

        var doors = Doors.ContainsKey(areaId) ? Doors[areaId] : null;
        if (doors == null)
        {
            Debug.LogWarning($"Custom area door opener {name} has no door in it");
            return;
        }

        if (isHere)
        {
            if (!WasHere.ContainsKey(areaId))
            {
                WasHere[areaId] = new List<GridEntity>();
            }

            if (WasHere[areaId].Contains(entity)) return;

            bool wasEmpty = !HasOccupants;

            WasHere[areaId].Add(entity);

            if (wasEmpty)
            {
                foreach (var door in doors)
                {
                    Debug.Log($"Custom area door opener opening {door}");
                    door.OpenDoor(entity);
                }

                Debug.Log($"Custom area door opener {name}: Opening doors");
            } else
            {
                Debug.Log($"Custom area door opener {name}: Not opening doors, becuase someone was here already");
            }
        } else
        {
            if (WasHere.ContainsKey(areaId))
            {
                if (WasHere[areaId].Remove(entity) && !HasOccupants)
                {
                    foreach (var door in doors)
                    {
                        Debug.Log($"Custom area door opener closing {door} ({door.Node.name})");
                        door.CloseDoor(entity);
                    }

                    Debug.Log($"Custom area door opener {name}: Closing doors");
                }
            }
        }
    }

    [ContextMenu("Info")]
    void Info()
    {
        var doors = Doors.ContainsKey(areaId) ? Doors[areaId].Count : 0;

        Debug.Log($"Custom area door opener '{name}' Area {areaId}:" +
            $"{doors} doors known, was here: {HasOccupants}. " +
            $"Opens for player({openForPlayer}), opens for enemy {openForEnemy}");
    }
}
