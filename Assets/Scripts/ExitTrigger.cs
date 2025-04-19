using LMCore.Crawler;
using LMCore.TiledDungeon;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledDungeon.Integration;
using LMCore.TiledImporter;
using UnityEngine;

public enum ExitType { MainExit, FireEscape, AnomalyDeath, BossDeath };

public delegate void ExitOfficeEvent(ExitType exitType);

public class ExitTrigger : TDFeature, ITDCustom
{
    [SerializeField, HideInInspector]
    Direction doorDirection;

    [SerializeField, HideInInspector]
    bool hideDoorPrompts;

    TDDoor door
    {
        get
        {
            Debug.Log($"ExitTrigger: Door should be in the {doorDirection} direction");
            var coords = doorDirection.Translate(Coordinates);
            var node = Dungeon[coords];
            if (node == null) return null;

            return node.GetComponentInChildren<TDDoor>();
        }
    }

    public static event ExitOfficeEvent OnExitOffice;

    [SerializeField, HideInInspector]
    ExitType exitType;

    private void OnEnable()
    {
        if (hideDoorPrompts)
        {
            door.SilenceAllPrompts = true;
        }
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
        TiledDungeon.OnDungeonUnload += TiledDungeon_OnDungeonUnload;
    }

    private void OnDisable()
    {
        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
        TiledDungeon.OnDungeonUnload -= TiledDungeon_OnDungeonUnload;
    }

    private void TiledDungeon_OnDungeonUnload(TiledDungeon dungeon)
    {
        capturedPlayer = null;
    }

    GridEntity capturedPlayer;

    private void GridEntity_OnPositionTransition(GridEntity entity)
    {
        if (capturedPlayer) return;

        if (entity.EntityType == GridEntityType.PlayerCharacter && entity.Coordinates == Coordinates)
        {
            door.OpenDoor(entity);

            entity.MovementBlockers.Add(this);

            // TODO: Some fancy animation
            capturedPlayer = entity;

            OnExitOffice?.Invoke(exitType);
        }
    }

    public void Configure(TDNode node, TiledCustomProperties properties)
    {
        var mainExit = properties.Bool("MainExit");
        exitType = mainExit ? ExitType.MainExit : ExitType.FireEscape;
        doorDirection = properties.Direction("DoorDirection", TDEnumDirection.None).AsDirection();
        hideDoorPrompts = properties.Bool("HideDoorPrompts", false);
    }
}
