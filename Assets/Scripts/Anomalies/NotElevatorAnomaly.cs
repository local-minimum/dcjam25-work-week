using LMCore.Crawler;
using LMCore.TiledDungeon;
using LMCore.TiledDungeon.DungeonFeatures;
using System.Linq;
using UnityEngine;

public class NotElevatorAnomaly : AbsAnomaly
{
    TDDoor door;

    [SerializeField]
    int wormHoleId;

    [SerializeField]
    bool oneShot = true;

    [SerializeField]
    AudioSource speaker;

    [SerializeField]
    TDDecoration managerSpawn;

    [SerializeField]
    TDDecoration managerTarget;

    protected override void OnDisableExtra()
    {
        door.OnDoorChange -= Door_OnDoorChange;
        if (speaker != null) speaker.Stop();
    }

    protected override void OnEnableExtra()
    {
        door = Node.GetComponentInChildren<TDDoor>();
        door.OnDoorChange += Door_OnDoorChange;
        if (speaker != null) speaker.Stop();
    }

    bool activeAnomaly;
    bool teleported;

    private void Door_OnDoorChange(TDDoor door, TDDoor.Transition transition, bool isOpen, GridEntity entity)
    {
        if (activeAnomaly &&
                entity != null &&
                entity.EntityType == GridEntityType.PlayerCharacter && 
                transition == TDDoor.Transition.Opening && 
                (!oneShot || !teleported))
        {
            var teleportTarget = Dungeon.FindTeleportersById(wormHoleId, onlyExit: true).FirstOrDefault();
            if (teleportTarget != null)
            {
                teleportTarget.ReceiveEntity(entity);
                // Is this really needed?
                teleportTarget
                    .Node
                    .AddOccupant(entity, push: true);

                teleported = true;

                TeleportManager();

                if (speaker != null)
                {
                    speaker.Play();
                }
            }
        }
    }

    void TeleportManager()
    {
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
                        Direction.South,
                        managerTarget.GetComponentInParent<TDPathCheckpoint>());
                }
            }
        }
    }

    protected override void SetAnomalyState()
    {
        activeAnomaly = true;
    }

    protected override void SetNormalState()
    {
        activeAnomaly = false;
        if (speaker != null) speaker.Stop();
    }
}
