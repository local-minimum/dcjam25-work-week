using Codice.CM.Common.Checkin.Partial;
using LMCore.Crawler;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledDungeon.Integration;
using LMCore.TiledImporter;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDTeleporter : TDFeature
    {
        [SerializeField, HideInInspector]
        TDEnumTransition _transition;

        public bool HasEntry => _transition.HasEntry();
        public bool HasExit => _transition.HasExit();

        [SerializeField, HideInInspector]
        Direction _exitDirection;
        public Direction ExitDirection => _exitDirection;

        [SerializeField, HideInInspector]
        int _wormholeId;
        public int WormholeId => _wormholeId;

        public void Configure(TiledCustomProperties props, TDEnumTransition transition)
        {
            _wormholeId = props == null ? 0 : props.Int(TiledConfiguration.instance.TeleporterIdProperty, 0);
            _exitDirection = props == null ? Direction.None : 
                props.Direction(TiledConfiguration.instance.DirectionKey, TDEnumDirection.None).AsDirection();

            _transition = transition;

            Info();
        }

        public void ReceiveEntity(GridEntity entity)
        {
            entity.MovementInterpreter.CancelMovement();

            entity.Coordinates = Coordinates;

            var transportationMode = entity.TransportationMode
                .RemoveFlag(TransportationMode.Climbing)
                .RemoveFlag(TransportationMode.Swimming)
                .RemoveFlag(TransportationMode.Squeezing)
                .AddFlag(TransportationMode.Teleporting);

            bool falling = false;
            if (Node.HasFloor)
            {
                entity.AnchorDirection = Direction.Down;
                transportationMode.AddFlag(TransportationMode.Walking);
            } else
            {
                entity.AnchorDirection = Direction.None;
                falling = true;
            }

            if (ExitDirection != Direction.None)
            {
                entity.LookDirection = ExitDirection;
                Debug.Log($"Teleporter Exit @ {Coordinates} sets {entity.name} looking {ExitDirection}");
            }

            // Protect against serial teleportations
            entity.TransportationMode = transportationMode; 

            if (entity.Falling != falling)
            {
                entity.Falling = falling;
            }

            entity.Sync();
        }

        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log($"Teleporter '{name}' @ {Coordinates}: Entry({HasEntry}) Exit({HasExit}) WormholeId({WormholeId}) Exit LookDirection({ExitDirection})");
        }
    }
}
