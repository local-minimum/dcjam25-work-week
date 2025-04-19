using LMCore.Crawler;
using LMCore.Extensions;
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

        if (exitType == ExitType.MainExit)
        {
            var door = this.door.transform;
            var parent = transform.parent;
            // Hide all the default walls, we just want the elevator
            for (int i = 0, n = parent.childCount; i<n; i++)
            {
                var sib = parent.GetChild(i);
                if (sib == transform || sib == door || sib.GetComponent<TDDecoration>() != null) continue;
                var rend = sib.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    rend.enabled = false;
                }
            }
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
        if (capturedPlayer || exiting) return;

        if (entity.EntityType == GridEntityType.PlayerCharacter && entity.Coordinates == Coordinates)
        {
            var door = this.door;
            if (!door.OpenOrOpening)
            {
                door.OpenDoor(entity);
            }

            door.SilenceAllPrompts = true;

            entity.MovementBlockers.Add(this);

            capturedPlayer = entity;

            if (exitType == ExitType.MainExit)
            {
                entity.InjectForcedMovement(LMCore.IO.Movement.YawCCW);
                entity.InjectForcedMovement(LMCore.IO.Movement.YawCCW, pushQueue: true);
                turning = true;
                exiting = true;
            } else
            {
                OnExitOffice?.Invoke(exitType);
            }
        }
    }

    public void Configure(TDNode node, TiledCustomProperties properties)
    {
        var mainExit = properties.Bool("MainExit");
        exitType = mainExit ? ExitType.MainExit : ExitType.FireEscape;
        doorDirection = properties.Direction("DoorDirection", TDEnumDirection.None).AsDirection();
        hideDoorPrompts = properties.Bool("HideDoorPrompts", false);
    }

    bool exiting;
    bool turning;
    float exitTime;

    [SerializeField, HelpBox("This value must be set in the prefab to stay", HelpBoxMessageType.Warning)]
    float waitForElevatorDoorClose = 2f;

    private void Update()
    {
        if (exitType != ExitType.MainExit || !exiting) return;

        if (turning && capturedPlayer.Moving == MovementType.Stationary)
        {
            var door = this.door;
            if (door.OpenOrOpening)
            {
                door.CloseDoor(capturedPlayer);
            }
            turning = false;
            exitTime = Time.timeSinceLevelLoad + waitForElevatorDoorClose;
        } else if (!turning && Time.timeSinceLevelLoad > exitTime)
        {
            exiting = false;
            OnExitOffice?.Invoke(exitType);
        }
    }
}
