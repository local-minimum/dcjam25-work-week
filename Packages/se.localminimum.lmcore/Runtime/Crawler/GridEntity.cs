using LMCore.Extensions;
using LMCore.Inventory;
using LMCore.IO;
using LMCore.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LMCore.Crawler
{
    public delegate void InteractEvent(GridEntity entity);
    public delegate void MoveEvent(GridEntity entity);
    public delegate void PositionTransitionEvent(GridEntity entity);

    [System.Flags]
    public enum MovementType
    {
        Stationary,
        Translating,
        Rotating
    };

    public class GridEntity : MonoBehaviour
    {
        public static event InteractEvent OnInteract;
        public static event MoveEvent OnMove;
        public static event PositionTransitionEvent OnPositionTransition;

        /// <summary>
        /// Way to reference the entity in other systems like dialogs and saves
        /// </summary>
        public string Identifier = System.Guid.NewGuid().ToString();

        protected string PrefixLogMessage(string message) => $"Entity '{name}' @ {Coordinates} anchor {AnchorDirection}/{AnchorMode} looking {LookDirection}: {message}";

        public override string ToString() =>
            $"Entity '{name}' @ {Coordinates} Anchor({AnchorDirection}/{AnchorMode}) Down({Down}) Looking({LookDirection})";

        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log($"{this} Falling({Falling}) Mode({TransportationMode}) Moving({Moving})");
        }

        [SerializeField]
        private AbsInventory _inventory;
        public AbsInventory Inventory => _inventory;

        public UnityEvent OnFall;
        public UnityEvent ContinueFall;
        public UnityEvent OnLand;
        public EntityAbilities Abilities;

        public GridEntityType EntityType;
        public IGridSizeProvider GridSizeProvider => GetComponentInParent<IGridSizeProvider>(true);
        public IDungeon Dungeon => GetComponentInParent<IDungeon>(true);

        private MovementType _moving;
        public MovementType Moving
        {
            get => _moving;
            set
            {
                var previousState = _moving;
                _moving = value;
                OnMove?.Invoke(this);
                if (value == MovementType.Stationary)
                {
                    if (Node != null)
                    {
                        Node.AfterMovement(this, previousState);
                    }
                }
            }
        }

        public HashSet<MonoBehaviour> MovementBlockers { get; set; } = new HashSet<MonoBehaviour>();
        public bool MovementBlocked => MovementBlockers.Count > 0;

        #region Anchorage
        public bool RotationRespectsAnchorDirection { get; set; } = false;

        [SerializeField, HideInInspector]
        private Anchor _anchor;
        public Anchor NodeAnchor
        {
            get => _anchor;
            set
            {
                if (_anchor != value)
                {
                    _anchor?.RemoveAnchor(this);
                }

                // We need to run add occupation checks even within node when changing
                // cube face anchor
                bool newAnchor = _anchor != value;
                bool addOccupant = false;

                if (value != null)
                {
                    value.AddAnchor(this);
                    if (Node != value.Node)
                    {
                        if (Node != null)
                        {
                            Node.RemoveOccupant(this);
                        }
                        addOccupant = true;
                    }

                    _node = null;
                    _anchorDirection = value.CubeFace;
                    TransportationMode = value.Traversal.ToTransportationMode();
                }
                else
                {
                    if (Node != null)
                    {
                        Node.RemoveOccupant(this);
                    }
                    // Only keep potential flying mode, we don't fly if we just fall
                    TransportationMode &= TransportationMode.Flying;
                }

                _anchor = value;
                if (addOccupant)
                {
                    // This needs to happen last so that the entity is fully in sync
                    value.Node.AddOccupant(this, false);
                }
                if (newAnchor) OnPositionTransition?.Invoke(this);
            }
        }

        [SerializeField, HideInInspector]
        private Direction _anchorDirection = Direction.Down;
        public Direction AnchorDirection
        {
            get
            {
                if (_anchor != null) return _anchor.CubeFace;
                return _anchorDirection;
            }
            set
            {
                bool newDirection = _anchorDirection != value;
                if (Node != null)
                {
                    var anchor = Node.GetAnchor(value);
                    if (anchor != null)
                    {
                        NodeAnchor = anchor;
                        return;
                    }

                    NodeAnchor?.RemoveAnchor(this);
                }

                _anchorDirection = value;
                _anchor = null;

                if (newDirection) OnPositionTransition?.Invoke(this);
            }
        }

        /// <summary>
        /// The direction of the anchor for the entity
        /// </summary>
        public Direction Down
        {
            get
            {
                if (RotationRespectsAnchorDirection && AnchorDirection != Direction.None)
                {
                    return _anchorDirection;
                }

                return Direction.Down;
            }
        }

        [SerializeField, HideInInspector]
        private IDungeonNode _node;
        public IDungeonNode Node
        {
            get
            {
                if (_anchor != null) return _anchor.Node;
                return _node;
            }

            set
            {
                bool newNode = false;
                var anchor = value?.GetAnchor(AnchorDirection);
                if (anchor != null)
                {
                    NodeAnchor = anchor;
                }
                else
                {
                    if (Node != value)
                    {
                        if (Node != null)
                        {
                            Node.RemoveOccupant(this);
                        }
                        if (_anchor != null)
                        {
                            _anchor.RemoveAnchor(this);
                        }
                        newNode = true;
                    }
                    _node = value;
                    _anchor = null;

                    // Only keep potential flying mode, we don't fly if we just fall
                    TransportationMode &= TransportationMode.Flying;
                }

                if (newNode)
                {
                    // This needs to happen last so that the entity is fully in sync
                    if (Node != null)
                    {
                        Node.AddOccupant(this, false);
                    }
                    OnPositionTransition?.Invoke(this);
                }
            }
        }

        private string AnchorMode
        {
            get
            {
                if (NodeAnchor != null) return "Achor";
                if (Node != null) return "Node";
                return "Dungeon";
            }
        }
        #endregion

        #region Coordinates
        /// <summary>
        /// Using XZ Plane, returns coordinates in 2D
        /// </summary>
        public Vector2Int Coordinates2D
        {
            get => Coordinates.To2DInXZPlane();
            set => Coordinates = value.To3DFromXZPlane(Elevation);
        }

        public int Elevation
        {
            get => Coordinates.y;
            set
            {
                Coordinates = new Vector3Int(_Coordinates.x, value, _Coordinates.z);
            }
        }

        [SerializeField, HideInInspector]
        Vector3Int _Coordinates;
        public Vector3Int Coordinates
        {
            get
            {
                if (_anchor) return _anchor.Node.Coordinates;
                if (_node != null) return _node.Coordinates;
                return _Coordinates;
            }
            set
            {
                bool newCoords = _Coordinates != value;

                _Coordinates = value;
                if (Dungeon.HasNodeAt(_Coordinates))
                {
                    var node = Dungeon[_Coordinates];
                    var anchor = node?.GetAnchor(AnchorDirection);
                    if (anchor != null)
                    {
                        NodeAnchor = anchor;
                    }
                    else
                    {
                        Node = node;
                    }
                }
                else
                {
                    _anchorDirection = AnchorDirection;
                    Node = null;
                    NodeAnchor = null;
                }

                if (newCoords) OnPositionTransition?.Invoke(this);
            }
        }
        #endregion

        [SerializeField, HideInInspector]
        private Direction _LookDirection;
        public Direction LookDirection
        {
            get => _LookDirection;
            set
            {
                var newDirection = _LookDirection != value;
                _LookDirection = value;

                if (newDirection) OnPositionTransition?.Invoke(this);
            }
        }

        /// <summary>
        /// Returns where and in what direction a grid entity would end up given a certain movenment
        /// without actually moving them.
        /// </summary>
        /// <param name="movement"></param>
        /// <param name="lookDirection"></param>
        /// <param name="down"></param>
        /// <returns></returns>
        public Vector3Int Project(Movement movement, out Direction lookDirection, out Direction down)
        {
            lookDirection = LookDirection.ApplyRotation(Down, movement, out down);
            return LookDirection.RelativeTranslation3D(Down, movement).Translate(Coordinates);
        }

        [SerializeField]
        Transform _lookTarget;
        /// <summary>
        /// When other entities do raycasts towards the entity use this transform to focus on
        /// </summary>
        public Transform LookTarget => _lookTarget ?? transform;

        public void TriggerPositionTransitionEvent() =>
            OnPositionTransition?.Invoke(this);

        public TransportationMode TransportationMode;


        private bool _falling;
        public bool Falling
        {
            get => _falling;
            set
            {
                if (value != _falling)
                {
                    if (value)
                    {
                        Debug.Log(PrefixLogMessage("Is falling"));
                        TransportationMode = TransportationMode.None;
                        OnFall?.Invoke();
                    }
                    else
                    {
                        Debug.Log(PrefixLogMessage("Stopped falling"));
                        TransportationMode = NodeAnchor != null && AnchorDirection == Direction.Down ? TransportationMode.Walking : TransportationMode.Flying;
                        OnLand?.Invoke();
                    }
                }
                else if (value)
                {
                    ContinueFall?.Invoke();
                }
                _falling = value;
            }
        }

        MovementInterpreter _MovementInterpreter;
        /// <summary>
        /// The interpreter of movements for the entity, gerneral for all types of entites.
        /// To force a movement, use `InjectMovement` instead.
        /// </summary>
        public MovementInterpreter MovementInterpreter
        {
            get
            {
                if (_MovementInterpreter == null)
                {
                    _MovementInterpreter = GetComponentInChildren<MovementInterpreter>();
                }
                return _MovementInterpreter;
            }
        }

        /// <summary>
        /// Player input system, use `InjectMovement` to force a movement for any type of entity
        /// </summary>
        public CrawlerInput Input => GetComponent<CrawlerInput>();

        /// <summary>
        /// Forces a movement upon the grid entity.
        /// 
        /// Normal rules for node exit and target node entry still apply
        ///
        /// Uses game clock for move duration
        /// </summary>
        public void InjectForcedMovement(Movement movement, bool pushQueue = false) =>
            InjectForcedMovement(movement, ElasticGameClock.instance.baseTickDuration, pushQueue);

        /// <summary>
        /// Forces a movement upon the grid entity.
        /// 
        /// Normal rules for node exit and target node entry still apply
        /// </summary>
        public void InjectForcedMovement(Movement movement, float duration, bool pushQueue = false)
        {
            var input = Input;
            if (input != null)
            {
                input.InjectMovement(movement, true, pushQueue: pushQueue);
            }
            else
            {
                MovementInterpreter.InvokeMovement(movement, duration, true);
            }
        }

        /// <summary>
        /// Updates position and rotation as well as occupying dungeon node at coordinates possible
        /// </summary>
        public void Sync()
        {
            if (GridSizeProvider == null)
            {
                Debug.LogWarning(PrefixLogMessage("have yet to recieve a grid size provider, ignoring sync"));
                return;
            }

            transform.position = Dungeon.Position(this);
            try
            {
                transform.rotation = LookDirection.AsQuaternion(Down, RotationRespectsAnchorDirection);
            }
            catch (System.ArgumentException e)
            {
                Debug.LogError(
                    PrefixLogMessage($"Can't parse look direction as rotation ({LookDirection} / 3D {RotationRespectsAnchorDirection}): {e.Message}"));
            }

            CheckFall();
        }

        public void Sync(MovementCheckpoint checkpoint, Direction movementDirection, bool forced)
        {
            if (checkpoint.Anchor != null)
            {
                NodeAnchor = checkpoint.Anchor;
            }
            else if (checkpoint.Node != null)
            {
                Node = checkpoint.Node;
            }
            else
            {
                Coordinates = checkpoint.Coordinates;
            }

            LookDirection = checkpoint.LookDirection;

            if (forced)
            {
                if (checkpoint.Node != null)
                {
                    checkpoint.Node.PushOccupants(this, movementDirection);
                }
            }

            Sync();

            if (checkpoint != null && checkpoint.Traversal.Either(AnchorTraversal.Conveyor, AnchorTraversal.ConveyorSqueeze))
            {
                var movement = movementDirection.AsMovement(LookDirection, Down);
                Debug.Log(PrefixLogMessage($"Conveyor causes movement {movement}"));
                InjectForcedMovement(movement);
            }
        }

        private void Start()
        {
            if (Node != null)
            {
                Debug.Log(PrefixLogMessage($"Waking up at {Node}"));
                Node.AddOccupant(this, true);
            }
            else if (Dungeon != null)
            {
                if (Dungeon.HasNodeAt(Coordinates))
                {
                    Debug.Log(PrefixLogMessage($"Waking up at {Coordinates}"));
                    Node = Dungeon[Coordinates];
                }
                else
                {
                    Debug.LogWarning(PrefixLogMessage($"Waking up outside dungeon at {Coordinates}"));
                }
            }
            else
            {
                Debug.LogWarning(PrefixLogMessage($"Waking up without a dungeon"));
            }
        }

        private void OnEnable()
        {
            AbsMenu.OnShowMenu += AbsMenu_OnShowMenu;
            AbsMenu.OnExitMenu += AbsMenu_OnExitMenu;
            var n = Node;
            if (n != null)
            {
                Debug.Log(PrefixLogMessage($"Added occupancy from {n}"));
                n.AddOccupant(this, true);
            }
        }

        private void OnDisable()
        {
            AbsMenu.OnShowMenu -= AbsMenu_OnShowMenu;
            AbsMenu.OnExitMenu -= AbsMenu_OnExitMenu;
            var n = Node;
            if (n != null)
            {
                n.RemoveOccupant(this, transferEntity: false);
                Debug.Log(PrefixLogMessage($"Removed occupancy from {n}"));
            }
        }

        private void AbsMenu_OnExitMenu(AbsMenu menu)
        {
            MovementBlockers.Remove(menu);
        }

        private void AbsMenu_OnShowMenu(AbsMenu menu)
        {
            if (menu.PausesGameplay)
            {
                MovementBlockers.Add(menu);
            }
        }

        public void Interact()
        {
            if (AbsMenu.PausingGameplay || MovementBlocked) return;

            Debug.Log(PrefixLogMessage("Interacting"));
            OnInteract?.Invoke(this);
        }

        public void CheckFall()
        {
            if (TransportationMode.HasFlag(TransportationMode.Flying) || TransportationMode.HasFlag(TransportationMode.Climbing))
            {
                Debug.Log(PrefixLogMessage("Ended its fall due to tranportation mode"));
                Falling = false;
                return;
            }

            var node = Dungeon[Coordinates];
            if (node == null)
            {
                Debug.LogWarning(PrefixLogMessage("Outside the map, assuming fall"));
                Falling = true;
                return;
            }

            if (!TransportationMode.HasFlag(TransportationMode.Flying) && !node.CanAnchorOn(this, AnchorDirection))
            {
                if (!node.HasFloor)
                {
                    Debug.Log(PrefixLogMessage("In the air -> falling"));
                    Falling = true;
                }
            }
            else if (Falling)
            {
                Debug.Log(PrefixLogMessage("Ended its fall"));
                Falling = false;
            }
        }

        public bool Alive { get; private set; } = true;

        public void Kill()
        {
            _Coordinates = Coordinates;
            Node = null;
            Alive = false;
        }
    }
}
