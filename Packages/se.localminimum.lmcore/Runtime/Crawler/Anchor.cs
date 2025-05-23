using LMCore.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Crawler
{
    public enum AnchorTraversal { None, Walk, Climb, Scale, Stairs, Conveyor, ConveyorSqueeze }
    public static class AnchorTraversalExtensions
    {
        public static TransportationMode ToTransportationMode(this AnchorTraversal traversal)
        {
            switch (traversal)
            {
                case AnchorTraversal.None:
                    return TransportationMode.None;
                case AnchorTraversal.Walk:
                case AnchorTraversal.Conveyor:
                case AnchorTraversal.Scale:
                case AnchorTraversal.Stairs:
                    return TransportationMode.Walking;
                case AnchorTraversal.Climb:
                    return TransportationMode.Climbing;
                case AnchorTraversal.ConveyorSqueeze:
                    return TransportationMode.Squeezing;
                default:
                    Debug.LogWarning($"{traversal} conversion to transportation mode not known");
                    return TransportationMode.None;

            }
        }

        public static bool CanBeTraversedBy(this AnchorTraversal traversal, GridEntity entity) =>
            traversal.CanBeTraversedBy(entity.Abilities.transportaionModes);

        public static bool CanBeTraversedBy(this AnchorTraversal traversal, TransportationMode mode) =>
            mode.HasFlag(traversal.ToTransportationMode());
    }

    public enum AnchorYRotation { None, CW, CCW, OneEighty }

    public static class AnchorYRotationExtensions
    {
        public static AnchorYRotation AsYRotation(this Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return AnchorYRotation.None;
                case Direction.South:
                    return AnchorYRotation.OneEighty;
                case Direction.East:
                    return AnchorYRotation.CW;
                case Direction.West:
                    return AnchorYRotation.CCW;
                default:
                    Debug.LogError($"Can't construction Y rotation from {direction}");
                    return AnchorYRotation.None;
            }
        }

        /// <summary>
        /// Rotates prefab configuration to dungeon rotation
        /// </summary>
        public static Direction Rotate(this AnchorYRotation rotation, Direction direction)
        {
            if (!direction.IsPlanarCardinal()) { return direction; }

            switch (rotation)
            {
                case AnchorYRotation.None:
                    return direction;
                case AnchorYRotation.CW:
                    return direction.RotateCW();
                case AnchorYRotation.CCW:
                    return direction.RotateCCW();
                default:
                    return direction.Inverse();
            }
        }

        /// <summary>
        /// Rotates dungeon rotation to prefab configuration
        /// </summary>
        public static Direction InverseRotate(this AnchorYRotation rotation, Direction direction)
        {
            if (!direction.IsPlanarCardinal()) { return direction; }

            switch (rotation)
            {
                case AnchorYRotation.None:
                    return direction;
                case AnchorYRotation.CW:
                    return direction.RotateCCW();
                case AnchorYRotation.CCW:
                    return direction.RotateCW();
                default:
                    return direction.Inverse();
            }
        }
    }

    public class Anchor : MonoBehaviour
    {
        protected string PrefixLogMessage(string message) =>
            $"Anchor {CubeFace} @ {Node?.Coordinates}: {message}";

        [HideInInspector]
        public AnchorYRotation PrefabRotation = AnchorYRotation.None;

        public AnchorTraversal Traversal;

        [SerializeField, Tooltip("Use None for center of cube")]
        private Direction _PrefabCubeFace = Direction.Down;

        public bool HasEdge(Direction direction) =>
            CubeFace != direction && CubeFace.Inverse() != direction;

        public void SetPrefabCubeFace(Direction direction)
        {
            PrefabRotation = AnchorYRotation.None;
            _PrefabCubeFace = direction;
        }

        public Direction CubeFace =>
            PrefabRotation.Rotate(_PrefabCubeFace);

        IDungeonNode _node;
        public IDungeonNode Node
        {
            get
            {
                if (_node == null || ManagingMovingCubeFace != null)
                {
                    _node = GetComponentInParent<IDungeonNode>(true);
                }
                return _node;
            }

            set { _node = value; }
        }

        public override string ToString() =>
            $"Anchor {CubeFace} ({Traversal}) of {Node}";

        IDungeon _dungeon;
        public IDungeon Dungeon
        {
            get
            {
                if (_dungeon == null)
                {
                    _dungeon = GetComponentInParent<IDungeon>();
                }
                return _dungeon;
            }

            set { _dungeon = value; }
        }

        EntityConstraint _constraint;
        public EntityConstraint Constraint
        {
            get
            {
                if (_constraint == null)
                {
                    _constraint = GetComponent<EntityConstraint>();
                }
                return _constraint;
            }
        }

        [SerializeField, Range(-1, 1)]
        float baseOffset;

        float HalfGridSize
        {
            get
            {
                var d = Dungeon;
                if (d == null) return 1.5f;

                return d.GridSize * 0.5f;
            }
        }

        float HalfGridHeight
        {
            get
            {
                var d = Dungeon;
                if (d == null) return 1.5f;

                return d.GridHeight * 0.5f;
            }
        }

        public IMovingCubeFace ManagingMovingCubeFace { get; set; }


        #region Managing Entiteis
        private HashSet<GridEntity> entities = new HashSet<GridEntity>();
        public void AddAnchor(GridEntity entity) =>
            entities.Add(entity);

        public void RemoveAnchor(GridEntity entity) =>
            entities.Remove(entity);
        #endregion

        #region WorldPositions
        Dictionary<Direction, PositionSentinel> _sentinels = null;
        Dictionary<Direction, PositionSentinel> Sentinels
        {
            get
            {
                if (_sentinels == null)
                {
                    _sentinels = new Dictionary<Direction, PositionSentinel>(
                        GetComponentsInChildren<PositionSentinel>()
                        .Select(s => new KeyValuePair<Direction, PositionSentinel>(
                            PrefabRotation.Rotate(s.Direction), s))
                    );
                }

                return _sentinels;
            }
        }

        bool TraversableEdge(GridEntity entity, Direction edge)
        {
            /*
            if (entity.EntityType == GridEntityType.Enemy)
            {
                Debug.Log($"{Node.Coordinates}:{edge} on {CubeFace}: Has({HasEdge(edge)}), Sentinel({Sentinels.GetValueOrDefault(edge)}) for entity ({entity.TransportationMode})");
            }
            */
            if (!HasEdge(edge)) return false;

            var sentinel = Sentinels.GetValueOrDefault(edge);
            if (sentinel == null) return true;

            return !sentinel.Blocked.HasFlag(entity.TransportationMode);
        }

#if UNITY_EDITOR
        float edgeDirectionToSize(Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                case Direction.East:
                case Direction.Up:
                    return 0.1f;
                case Direction.Down:
                case Direction.West:
                case Direction.South:
                    return 0.075f;
                default:
                    return 0.05f;
            }
        }

        Dictionary<Direction, Color> directionToColor = new Dictionary<Direction, Color>() {
            { Direction.North, Color.blue },
            { Direction.South, Color.white},
            { Direction.West, Color.green },
            { Direction.East, Color.yellow },
            { Direction.Up, Color.cyan},
            { Direction.Down, Color.magenta },
            { Direction.None, Color.red },
        };

        private void OnDrawGizmosSelected()
        {
            var a = CubeFace;
            Gizmos.color = directionToColor[a];
            Gizmos.DrawWireCube(CenterPosition, Vector3.one * 0.3f);

            foreach (var direction in DirectionExtensions.AllDirections)
            {
                if (!HasEdge(direction)) continue;
                Gizmos.color = directionToColor[direction];
                Gizmos.DrawWireSphere(GetEdgePosition(direction), edgeDirectionToSize(direction));
            }
        }
#endif

        [ContextMenu("Refresh sentinels in editor")]
        void RefreshSentinels()
        {
            _sentinels = null;
            Debug.Log(PrefixLogMessage($"Rotating sentinels by {PrefabRotation}"));
        }

        public Vector3 CenterPosition
        {
            get
            {
                var halfSize = HalfGridSize;
                var halfHeight = HalfGridHeight;

                var s = Sentinels;
                if (s != null && s.ContainsKey(Direction.None))
                {
                    return s[Direction.None].Position;
                }

                var offset = CubeFace.AsLookVector3D().ToDirection(halfSize + baseOffset, halfHeight + baseOffset);

                if (ManagingMovingCubeFace != null)
                {
                    return ManagingMovingCubeFace.VirtualNodeCenter + offset;
                }

                var n = Node;
                if (n != null)
                {
                    return n.CenterPosition + offset;
                }

                return (Vector3.up * halfSize) + offset;
            }
        }


        public Vector3 GetEdgePosition(Direction direction)
        {
            if (direction == Direction.None || direction == CubeFace)
                return CenterPosition;

            if (direction == CubeFace.Inverse())
            {
                Debug.LogWarning(PrefixLogMessage("Requesting inverse of anchor, returning center"));
                return CenterPosition;
            }

            if (Sentinels.ContainsKey(direction)) return Sentinels[direction].Position;

            return CenterPosition + direction.AsLookVector3D().ToDirection(HalfGridSize, HalfGridHeight);
        }
        #endregion

        /// <summary>
        /// Check if there's a neighbour in the direction with a vertical offset
        /// </summary>
        /// <param name="direction">Main direction of the movement</param>
        /// <param name="offset">Vertical offset</param>
        /// <param name="entity">Entity in question, only used for tranportaion mode</param>
        private Anchor GetNeighbour(Direction direction, Direction offset, GridEntity entity)
        {
            var dungeon = Dungeon;
            var node = Node;
            var neighbourCoordinates = offset.Translate(node.Neighbour(direction));

            if (dungeon != null && dungeon.HasNodeAt(neighbourCoordinates))
            {
                var neighbourNode = Dungeon[neighbourCoordinates];
                if (neighbourNode != null)
                {
                    var neighbourAnchor = neighbourNode.GetAnchor(CubeFace);
                    if (neighbourAnchor != null)
                    {
                        if (!neighbourAnchor.TraversableEdge(entity, direction.Inverse()))
                        {
                            return null;
                        }
                    }

                    return neighbourAnchor;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns anchor as if rounding an outer corner, if exists.
        /// 
        /// Note: This function doesn't check if there are other transitions that should
        /// have higher priority, such as normal tile to tile neighbours on the same level
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        private Anchor GetOuterCornerAnchor(Direction direction, GridEntity entity)
        {
            var node = Node;
            var intermediaryCoordinates = node.Neighbour(direction);
            var secondaryDirection = direction.PitchDown(CubeFace, out var _);
            var targetCoordinates = secondaryDirection.Translate(intermediaryCoordinates);

            var targetNode = Dungeon[targetCoordinates];
            if (targetNode == null) return null;

            var targetAnchor = targetNode.GetAnchor(direction.Inverse());
            if (targetAnchor == null) return null;

            var edgePosition = GetEdgePosition(direction);
            var offset = targetAnchor.GetEdgePosition(secondaryDirection.Inverse()) - edgePosition;

            var orthoDistance = Vector3.Project(offset, CubeFace.AsLookVector3D()).magnitude;
            var paraDistance = Vector3.Project(offset, direction.AsLookVector3D()).magnitude;

            /*
            Debug.Log(PrefixLogMessage($"Outer corner neighbour in {direction}/{secondaryDirection} is {targetAnchor}. " +
                $"{orthoDistance} < {entity.Abilities.maxScaleHeight} && {paraDistance} < {entity.Abilities.maxForwardJump}"));
            */

            if (orthoDistance < entity.Abilities.maxScaleHeight) // && paraDistance < entity.Abilities.maxForwardJump)
            {
                return targetAnchor;
            }

            return null;
        }

        public Anchor GetNeighbour(Direction direction, GridEntity entity, out MovementOutcome outcome)
        {
            if (!TraversableEdge(entity, direction))
            {
                outcome = MovementOutcome.Blocked;
                return null;
            }

            var node = Node;
            if (node != null)
            {
                var sameNodeNeighbour = node.GetAnchor(direction);

                var down = CubeFace.AsLookVector3D();
                var edgePosition = GetEdgePosition(direction);

                // deals with normal level transitions and ramps
                var candidates = new List<Direction>() {
                    Direction.None,
                    CubeFace,
                    CubeFace.Inverse(),
                }
                    .Select(offset => new
                    {
                        offset,
                        neighbour = GetNeighbour(direction, offset, entity)
                    })
                    .Where(candidate => candidate.neighbour != null)
                    .Select(candidate => new
                    {
                        candidate.offset,
                        candidate.neighbour,
                        orthoDistance = Vector3.Project(
                            candidate.neighbour.GetEdgePosition(direction.Inverse()) - edgePosition,
                            down).magnitude
                    })
                    .OrderBy(candidate => candidate.orthoDistance)
                    .ToList();

                if (candidates.Count > 0)
                {
                    var closest = candidates[0];
                    // Debug.Log(PrefixLogMessage($"My {direction} neigbour is {closest}"));

                    if (closest.offset == CubeFace.Inverse() && closest.orthoDistance <= entity.Abilities.maxScaleHeight)
                    {
                        // We seem to be going "up" one level, but to be able to do that
                        // we need the opposing of cube side to be free
                        sameNodeNeighbour = node.GetAnchor(CubeFace.Inverse());
                        if (sameNodeNeighbour != null)
                        {
                            if (sameNodeNeighbour.Traversal.CanBeTraversedBy(entity))
                            {
                                outcome = MovementOutcome.NodeInternal;
                                return sameNodeNeighbour;
                            }

                            outcome = MovementOutcome.Blocked;
                            return this;
                        }

                        if (closest.neighbour.Traversal.CanBeTraversedBy(entity))
                        {
                            outcome = MovementOutcome.NodeExit;
                            return closest.neighbour;
                        }

                        outcome = MovementOutcome.Blocked;
                        return this;
                    }

                    // if we are going "down" we really can't have an anchor in
                    // our exit direction
                    if (sameNodeNeighbour)
                    {
                        if (sameNodeNeighbour.Traversal.CanBeTraversedBy(entity))
                        {
                            outcome = MovementOutcome.NodeInternal;
                            return sameNodeNeighbour;
                        }
                        else
                        {
                            outcome = MovementOutcome.Refused;
                            return this;
                        }
                    }

                    // even if going "down" one elevation is closer, we can't have
                    // something on the same level blocking our path
                    if (closest.offset == CubeFace)
                    {
                        var directNeighbour = candidates.Find(candidate => candidate.offset == Direction.None);
                        if (directNeighbour != null)
                        {
                            if (directNeighbour.neighbour.Traversal.CanBeTraversedBy(entity))
                            {
                                outcome = MovementOutcome.NodeExit;
                                return directNeighbour.neighbour;
                            }
                            else
                            {
                                outcome = MovementOutcome.Blocked;
                                return this;
                            }
                        }
                    }

                    // If the closest is "down" we should only count it as a neighbour
                    // if it is close. Else we would void ladders and such.
                    if (closest.offset == Direction.None || closest.orthoDistance <= entity.Abilities.maxScaleHeight)
                    {
                        if (closest.neighbour.Traversal.CanBeTraversedBy(entity))
                        {
                            outcome = MovementOutcome.NodeExit;
                            return closest.neighbour;
                        }
                        else
                        {
                            outcome = MovementOutcome.Blocked;
                            return this;
                        }
                    }
                }

                // Getting on and off at the top of ladders as an example
                var outerCornerAnchor = GetOuterCornerAnchor(direction, entity);
                if (outerCornerAnchor != null)
                {
                    if (outerCornerAnchor.Traversal.CanBeTraversedBy(entity))
                    {
                        Debug.Log(PrefixLogMessage($"Doing outer corner anchor move for {entity.name} to {outerCornerAnchor.Node.Coordinates} {outerCornerAnchor.CubeFace}"));
                        outcome = MovementOutcome.NodeExit;
                        return outerCornerAnchor;
                    }
                    else
                    {
                        outcome = MovementOutcome.Blocked;
                        return this;
                    }
                }

                if (sameNodeNeighbour != null)
                {
                    if (sameNodeNeighbour.Traversal.CanBeTraversedBy(entity))
                    {
                        outcome = MovementOutcome.NodeInternal;
                        return sameNodeNeighbour;
                    }
                    else
                    {
                        outcome = MovementOutcome.Blocked;
                        return this;
                    }
                }
            }

            outcome = MovementOutcome.NodeExit;
            return null;
        }

        [ContextMenu("Info")]
        void Info()
        {
            var entity = entities.FirstOrDefault() ?? Dungeon.Player;
            var neighbours = DirectionExtensions.AllDirections
                .Where(direction => HasEdge(direction))
                .Select(direction =>
                {
                    var neighbour = GetNeighbour(direction, entity, out var outcome);
                    if (neighbour == null)
                    {
                        if (!TraversableEdge(entity, direction))
                        {
                            return $"{direction} -> BLOCKED";
                        }
                        return $"{direction} -> NONE";
                    }
                    return $"{direction} -> {neighbour.Node.Coordinates}/{neighbour.CubeFace}/{outcome}";
                });

            Debug.Log(PrefixLogMessage(
                $"Managed entities: {string.Join(", ", entities.Select(e => e.name))}\n" +
                $"Neighbours for {entity.name}: {string.Join(" | ", neighbours)}"));
        }
    }
}
