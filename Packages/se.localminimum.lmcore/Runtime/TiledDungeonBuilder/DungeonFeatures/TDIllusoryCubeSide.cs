using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.IO;
using LMCore.TiledDungeon.Integration;
using LMCore.TiledDungeon.SaveLoad;
using LMCore.UI;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public delegate void DiscoverIllusionEvent(Vector3Int position, Direction direction);

    public class TDIllusoryCubeSide : TDFeature, IOnLoadSave
    {
        public static event DiscoverIllusionEvent OnDiscoverIllusion;

        public override string ToString() =>
            $"Illusory {direction} Side of {Node.Coordinates} is {(Discovered ? "discovered" : "undiscovered")}";

        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log(ToString());
        }

        [SerializeField, HideInInspector]
        Direction direction;
        public Direction CubeFace => direction;

        [SerializeField]
        string DiscoverTrigger = "Discover";

        [SerializeField]
        string DiscoveredTrigger = "Discovered";

        [SerializeField]
        Animator animator;

        [SerializeField]
        bool disableChildrenOnDiscovered;

        bool Discovered;

        int IOnLoadSave.OnLoadPriority => 500;

        public void Configure(Direction direction)
        {
            this.direction = direction;
        }

        private void Start()
        {
            InitStartCoordinates();
        }

        private void OnEnable()
        {
            OnDiscoverIllusion += TDIllusoryCubeSide_OnDiscoverIllusion;
            GridEntity.OnMove += GridEntity_OnMove;

            AbsMenu.OnShowMenu += HandleMenusPausing;
            AbsMenu.OnExitMenu += HandleMenusPausing;
        }

        private void OnDisable()
        {
            OnDiscoverIllusion -= TDIllusoryCubeSide_OnDiscoverIllusion;
            GridEntity.OnMove -= GridEntity_OnMove;

            AbsMenu.OnShowMenu -= HandleMenusPausing;
            AbsMenu.OnExitMenu -= HandleMenusPausing;
        }

        private void HandleMenusPausing(AbsMenu menu)
        {
            if (animator == null) return;

            if (AbsMenu.PausingGameplay == animator.enabled)
            {
                animator.enabled = !AbsMenu.PausingGameplay;
            }
        }

        bool underConsideration;
        Vector3Int movementStart;

        bool DidPassIllusion(Vector3Int movementEnd)
        {
            var direction = (movementEnd - movementStart).AsDirectionOrNone();

            // Debug.Log($"{this}: {direction}, start({movementStart}) end({movementEnd}) vs {Coordinates}");

            return (movementStart == Coordinates && direction == this.direction) ||
                (movementEnd == Coordinates && direction.Inverse() == this.direction);
        }

        private void GridEntity_OnMove(GridEntity entity)
        {
            if (entity.EntityType != GridEntityType.PlayerCharacter || Discovered) { return; }

            if (entity.Moving == MovementType.Stationary)
            {
                if (DidPassIllusion(entity.Coordinates))
                {
                    Discovered = true;
                    if (animator != null)
                    {
                        animator.SetTrigger(DiscoverTrigger);
                    }
                    if (disableChildrenOnDiscovered)
                    {
                        transform.HideAllChildren();
                    }
                    OnDiscoverIllusion?.Invoke(Coordinates, direction);
                }
            }
            else if (entity.Moving.HasFlag(MovementType.Translating))
            {
                movementStart = entity.Coordinates;
                underConsideration = movementStart.ChebyshevDistance(Coordinates) == 1;
            }
            else
            {
                underConsideration = false;
            }
        }

        private void TDIllusoryCubeSide_OnDiscoverIllusion(Vector3Int position, Direction direction)
        {
            var inverseDirection = direction.Inverse();

            if (!Discovered && direction.Translate(position) == Coordinates && this.direction == inverseDirection)
            {
                Discovered = true;
                if (animator != null)
                {
                    animator.SetTrigger(DiscoverTrigger);
                }
                if (disableChildrenOnDiscovered)
                {
                    transform.HideAllChildren();
                }
            }
        }

        public IllusionSave Save() => new IllusionSave()
        {
            position = StartCoordinates,
            discovered = Discovered,
            direction = direction,
        };

        void OnLoadGameSave(GameSave save)
        {
            if (save == null)
            {
                return;
            }

            var lvl = Node.Dungeon.MapName;

            Discovered = save.levels[lvl]
                ?.illusions
                .FirstOrDefault(ill => ill.position == StartCoordinates && ill.direction == direction)
                ?.discovered ?? false;

            if (Discovered)
            {
                if (animator != null)
                {
                    animator.SetTrigger(DiscoveredTrigger);
                }
                if (disableChildrenOnDiscovered)
                {
                    transform.HideAllChildren();
                }
            }
        }

        public void OnLoad<T>(T save) where T : new()
        {
            if (save is GameSave)
            {
                OnLoadGameSave(save as GameSave);
            }
        }
    }
}
