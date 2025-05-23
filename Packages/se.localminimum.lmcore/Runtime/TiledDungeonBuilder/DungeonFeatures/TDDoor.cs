using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.Inventory;
using LMCore.IO;
using LMCore.TiledDungeon.Integration;
using LMCore.TiledDungeon.SaveLoad;
using LMCore.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public delegate void DoorChangeEvent(TDDoor door, TDDoor.Transition transition, bool isOpen, GridEntity entity);

    /// <summary>
    /// Configures a door
    /// 
    /// Tiled Configuration:
    /// 
    /// * <c>TiledConfiguration.DoorClass</c>: The class for door properties
    ///   - <c>TiledConfiguration.InteractionDirectionKey</c> (direction): Limit 
    ///     direction that allows unlocking and/or opening/closing
    ///   - <c>TiledConfiguration.ObjManagedKey</c> (bool): If something else controls
    ///   the door rather than normal things.
    ///   - <c>TiledConfiguration.ObjAllowPlayerInteractions</c> (bool): If managed door
    ///   still can be opened and closed by the player. Useful for triggers automatically
    ///   opening doors for enemies.
    /// * <c>TiledConfiguration.ObjLockItemClass</c>: The class of the
    ///   of the object that decides key properties and such
    ///   - <c>TiledConfiguration.KeyKey</c> (string): Key Id to unlock door
    ///   - <c>TiledConfiguration.ConusumesKeyKey</c> (bool): If unlocking consumes key
    /// </summary>
    public class TDDoor : TDFeature, IOnLoadSave
    {
        public event DoorChangeEvent OnDoorChange;

        public enum Transition { None, Opening, Closing };

        public bool SilenceAllPrompts { get; set; }

        [SerializeField]
        bool silenceManageDoorPrompt;

        [SerializeField]
        bool allowInteractionWhileMoving = true;

        [SerializeField, HideInInspector]
        bool isOpen = false;

        [SerializeField, HideInInspector]
        TileModification[] modifications;

        [SerializeField, Header("Animations")]
        float positioningOffset = 1.2f;

        [SerializeField]
        string OpenAnimation;

        [SerializeField]
        string CloseAnimation;

        [SerializeField]
        string OpenedAnimation;

        [SerializeField]
        string ClosedAnimation;

        [SerializeField]
        GameObject KeyReaderNorth;

        [SerializeField]
        GameObject KeyReaderSouth;

        [SerializeField]
        Animator anim;

        [SerializeField]
        int animLayer;

        [SerializeField, Header("Sounds")]
        AudioSource speaker;

        [SerializeField]
        List<AudioClip> openSounds = new List<AudioClip>();

        [SerializeField, Header("Readers")]
        Image ReaderImageNorth;

        [SerializeField]
        Image ReaderImageSouth;

        [SerializeField]
        Sprite lockedSprite;
        [SerializeField]
        Color lockedColor;

        [SerializeField]
        Sprite closedSprite;
        [SerializeField]
        Color closedColor;

        [SerializeField]
        Sprite openedSprite;
        [SerializeField]
        Color openedColor;

        /// <summary>
        /// If door can only be opened/closed or unlocked from a particular direction.
        ///
        /// Set to None, it implies it has no extra restriction
        /// </summary>
        [SerializeField, HideInInspector]
        Direction interactionDirectionLimitation = Direction.None;

        /// <summary>
        /// If door effectively works like a thin wall, what side it is on.
        /// </summary>
        [SerializeField, HideInInspector]
        Direction edge = Direction.None;

        bool isLocked;

        string key;

        bool consumesKey;

        bool automaticTrapDoor;
        bool isTrapdoor;
        bool managed;

        [SerializeField, Header("Timing")]
        float autoCloseTime = 0.5f;

        public override string ToString() =>
            $"<{(isTrapdoor ? "Trap-" : "")}Door " +
            $"Axis({TraversalAxis}) " +
            $"Blocking({BlockingPassage}) " +
            $"Transition({ActiveTransition}) " +
            $"InteractionDirLim({interactionDirectionLimitation}) " +
            $"Open({isOpen})" +
            $"{(managed ? " Managed": "")}>";

        protected string PrefixLogMessage(string message) => $"Door @ {Coordinates}: {message}";

        [ContextMenu("Info")]
        void Info() => Debug.Log(this);

        Transition _activeTransition;
        Transition ActiveTransition
        {
            get
            {
                /*
                if (_activeTransition != Transition.None)
                {
                    var info = anim.GetCurrentAnimatorStateInfo(animLayer);
                    // TODO: This is a hack but something isn't right with normalized time
                    Debug.Log($"Door {_activeTransition} has normalized time: {info.normalizedTime}");
                    if (info.normalizedTime == 1f || info.length < 0.25f && !info.loop)
                    {
                        _activeTransition = Transition.None;
                    }
                }
                */

                return _activeTransition;
            }
        }

        public bool BlockingPassage
        {
            get
            {
                if (ActiveTransition == Transition.Closing)
                {
                    Debug.Log(PrefixLogMessage("closing"));
                    return true;
                }
                else if (TraversalAxis == DirectionAxis.UpDown && ActiveTransition == Transition.Opening)
                {
                    // We are a trapdoor we need to let player fall through
                    return false;
                }
                /*
                else if (ActiveTransition == Transition.Opening)
                {
                }
                */

                return !isOpen;
            }
        }

        public bool FullyClosed => !isOpen && ActiveTransition == Transition.None;
        public bool OpenOrOpening => isOpen && ActiveTransition == Transition.None || 
            ActiveTransition == Transition.Opening;

        public DirectionAxis TraversalAxis
        {
            get
            {
                return modifications
                    .First()
                    .Tile
                    .CustomProperties
                    .Orientation(TiledConfiguration.instance.TraversalAxisKey, TDEnumOrientation.None)
                    .AsAxis();
            }
        }

        public int OnLoadPriority => 500;

        bool synced = false;

        private void Start()
        {
            if (!synced)
            {
                SyncDoor();
            }
        }

        public void Configure(
            TileModification[] modifications,
            Direction edge,
            Direction interactionDirectionLimitation

        )
        {
            if (edge != Direction.None)
            {
                transform.position += (Vector3)edge.AsLookVector3D() * positioningOffset;
            }
            this.edge = edge;

            this.modifications = modifications;
            this.interactionDirectionLimitation = interactionDirectionLimitation;

            SyncDoor();
        }

        private void OnEnable()
        {
            GridEntity.OnInteract += GridEntity_OnInteract;
            GridEntity.OnMove += GridEntity_OnMove;
            GridEntity.OnPositionTransition += CheckShowPrompt;

            TDNode.OnNewOccupant += TDNode_OnNewOccupant;

            AbsMenu.OnShowMenu += HandleMenusPausing;
            AbsMenu.OnExitMenu += HandleMenusPausing;
        }

        private void OnDisable()
        {
            GridEntity.OnInteract -= GridEntity_OnInteract;
            GridEntity.OnMove -= GridEntity_OnMove;
            GridEntity.OnPositionTransition -= CheckShowPrompt;

            TDNode.OnNewOccupant -= TDNode_OnNewOccupant;

            AbsMenu.OnShowMenu -= HandleMenusPausing;
            AbsMenu.OnExitMenu -= HandleMenusPausing;
        }

        private void HandleMenusPausing(AbsMenu menu)
        {
            if (menu.PausesGameplay)
            {
                anim.StopPlayback();
            } else
            {
                anim.StartPlayback();
            }
        }

        bool ValidInteractionPosition(GridEntity entity, out bool nextToDoor)
        {
            bool validPosition = false;
            if (edge == Direction.None)
            {
                validPosition = entity.LookDirection.Translate(entity.Coordinates) == Coordinates;
            } else
            {
                validPosition = entity.Coordinates == Coordinates && entity.LookDirection == edge ||
                    entity.LookDirection == edge.Inverse() && entity.LookDirection.Translate(entity.Coordinates) == Coordinates;
            }

            if (!validPosition)
            {
                nextToDoor = false;
                return false;
            }

            nextToDoor = true;
            return interactionDirectionLimitation == Direction.None ||
                entity.LookDirection == interactionDirectionLimitation.Inverse();
        }

        string lastPrompt;
        private void CheckShowPrompt(GridEntity entity)
        {
            if (isTrapdoor || entity.EntityType != GridEntityType.PlayerCharacter) return;

            if (!ValidInteractionPosition(entity, out var nextToDoor))
            {
                if (!nextToDoor)
                {
                    HideLastPrompt();
                    return;
                }
            }

            if (allowInteractionWhileMoving || entity.Moving == MovementType.Stationary)
            {
                ShowPrompt(entity);
            }
        }

        private void ShowPrompt(GridEntity entity)
        {
            if (isTrapdoor) return;

            if (entity == null) return;

            var couldInteract = ValidInteractionPosition(entity, out var nextToDoor);
            if (!nextToDoor) return;

            var keyHint = InputBindingsManager
                .InstanceOrResource("InputBindingsManager")
                .GetActiveActionHint(GamePlayAction.Interact);

            var previousPrompt = lastPrompt;

            if (managed)
            {
                if (isOpen) return;
                lastPrompt = silenceManageDoorPrompt ? null : "Door controlled by unknown mechanism";
            }
            else if (isLocked)
            {
                var keyHolder = entity
                    .GetComponentsInChildren<AbsInventory>()
                    .FirstOrDefault(i => i.HasItem(key));

                if (keyHolder == null || !couldInteract)
                {
                    lastPrompt = "Door locked";
                }
                else
                {
                    lastPrompt = $"{keyHint} Unlock door";
                }
            }
            else if (isOpen)
            {
                if (!couldInteract) return;

                lastPrompt = $"{keyHint} Close door";
            }
            else if (FullyClosed)
            {
                lastPrompt = couldInteract ?
                    $"{keyHint} Open door" : "Door closed";
            }

            if (previousPrompt != lastPrompt)
            {
                if (!string.IsNullOrEmpty(previousPrompt))
                {
                    PromptUI.instance.RemoveText(previousPrompt);
                }

                if (!SilenceAllPrompts && !string.IsNullOrEmpty(lastPrompt))
                {
                    PromptUI.instance.ShowText(lastPrompt);
                }
            }
        }

        private void HideLastPrompt()
        {
            if (!string.IsNullOrEmpty(lastPrompt))
            {
                PromptUI.instance.RemoveText(lastPrompt);
                lastPrompt = null;
            }
        }

        private void GridEntity_OnMove(GridEntity entity)
        {
            if (entity.Moving != MovementType.Stationary)
            {
                activelyMovingEntities.Add(entity);
                if (!allowInteractionWhileMoving) HideLastPrompt();
            }
            else
            {
                activelyMovingEntities.Remove(entity);
                if (!allowInteractionWhileMoving) CheckShowPrompt(entity);
            }
        }

        HashSet<GridEntity> trapTriggeringEntities = new HashSet<GridEntity>();

        private void TDNode_OnNewOccupant(TDNode node, GridEntity entity)
        {
            if (entity.Coordinates != Coordinates)
            {
                if (trapTriggeringEntities.Contains(entity))
                {
                    trapTriggeringEntities.Remove(entity);

                    if (trapTriggeringEntities.Count == 0 && (isOpen || ActiveTransition == Transition.Opening))
                    {
                        StartCoroutine(AutoClose(PrefixLogMessage($"automatically closes after {entity.name}")));
                    }
                }

                return;
            }

            if (entity.TransportationMode.HasFlag(TransportationMode.Flying)
                || entity.AnchorDirection != Direction.Down) return;

            if (automaticTrapDoor && ActiveTransition != Transition.Opening && !isOpen)
            {
                trapTriggeringEntities.Add(entity);
                OpenDoor(null);
                entity.Falling = true;
                Debug.Log(PrefixLogMessage($"automatically opens for {entity.name}"));
            }
        }


        bool AutomaticTrapdoorAction(GridEntity entity)
        {
            return automaticTrapDoor
                && entity.Coordinates == Coordinates
                && entity.AnchorDirection == Direction.Down
                && !entity.TransportationMode.HasFlag(TransportationMode.Climbing)
                && !entity.TransportationMode.HasFlag(TransportationMode.Flying);
        }

        IEnumerator<WaitForSeconds> AutoClose(string logMessage)
        {
            yield return new WaitForSeconds(autoCloseTime);
            if (isOpen || ActiveTransition == Transition.Opening)
            {
                CloseDoor(null);
                if (!string.IsNullOrEmpty(logMessage)) Debug.Log(logMessage);
            }
        }

        HashSet<GridEntity> activelyMovingEntities = new();

        private void GridEntity_OnInteract(GridEntity entity)
        {
            if (isTrapdoor || managed) return;

            var onTheMove = activelyMovingEntities.Contains(entity);
            var validPosition = ValidInteractionPosition(entity, out bool nextToDoor);
            if (!nextToDoor) return;

            if ((!onTheMove || allowInteractionWhileMoving) && validPosition)
            {
                Debug.Log(PrefixLogMessage("Attempting to open door"));

                HideLastPrompt();

                if (isLocked)
                {
                    var keyHolder = entity != null ? entity
                        .GetComponentsInChildren<AbsInventory>()
                        .FirstOrDefault(i => i.HasItem(key)) : null;

                    if (keyHolder == null)
                    {
                        Debug.LogWarning(PrefixLogMessage($"requires key ({key})"));
                        return;
                    }

                    if (consumesKey)
                    {
                        if (keyHolder.Consume(key, out string _))
                        {
                            if (!SilenceAllPrompts)
                            {
                                PromptUI.instance.ShowText("Lost key", 2);
                            }
                        }
                        else
                        {
                            Debug.LogWarning(PrefixLogMessage($"Failed to consume key {key} from {keyHolder}"));
                        }
                    }
                    isLocked = false;
                    isOpen = false;
                    SyncImage(ReaderImageSouth, false, false);
                    SyncImage(ReaderImageNorth, false, false);
                } else
                {
                    Interact(entity);
                }

            }
        }

        GridEntity interactingEntity;

        public void CloseDoor(GridEntity entity)
        {
            var transition = ActiveTransition;
            if (transition == Transition.Closing) return;

            _activeTransition = Transition.Closing;
            OnDoorChange?.Invoke(this, ActiveTransition, isOpen, entity);

            // Debug.Log(PrefixLogMessage("Animating as closing"));
            anim.ResetTrigger(OpenAnimation);
            anim.SetTrigger(CloseAnimation);

            SyncImage(ReaderImageSouth, false, false);
            SyncImage(ReaderImageNorth, false, false);

            interactingEntity = entity;
        }

        [SerializeField]
        float considerOpenAfterProgress = 0.4f;
        float openingStart;

       public void OpenDoor(GridEntity entity)
        {
            var transition = ActiveTransition;
            if (transition == Transition.Opening) return;

            _activeTransition = Transition.Opening;
            OnDoorChange?.Invoke(this, ActiveTransition, isOpen, entity);

            // Debug.Log(PrefixLogMessage("Animating as opening"));
            anim.ResetTrigger(CloseAnimation);
            anim.SetTrigger(OpenAnimation);
            if (speaker != null && openSounds.Count > 0)
            {
                speaker.PlayOneShot(openSounds.GetRandomElement());
            }
            openingStart = Time.timeSinceLevelLoad;

            SyncImage(ReaderImageSouth, true, false);
            SyncImage(ReaderImageNorth, true, false);

            interactingEntity = entity;
        }

        [ContextMenu("Interact")]
        public void Interact() => Interact(null);
        public void Interact(GridEntity entity)
        {
            if (entity.MovementBlocked) return;

            Debug.Log(PrefixLogMessage($"Toggling door from Open({isOpen} / {ActiveTransition})"));
            switch (ActiveTransition)
            {
                case Transition.None:
                    if (isOpen)
                    {
                        CloseDoor(entity);
                    }
                    else
                    {
                        OpenDoor(entity);
                    }
                    break;
                case Transition.Opening:
                    CloseDoor(entity);
                    break;
                case Transition.Closing:
                    OpenDoor(entity);
                    break;
                default:
                    Debug.LogError(PrefixLogMessage($"Unhandled transition: {ActiveTransition}"));
                    break;
            }
        }

        void SyncDoor(bool triggerAnimations = true)
        {
            if (synced) return;

            var node = Node;
            if (node == null) return;

            InitStartCoordinates();

            var config = node.Config;

            var toggleGroups = config
                .GetObjectValues(
                    TiledConfiguration.instance.ObjToggleGroupClass,
                    props => props.Int(TiledConfiguration.instance.ObjGroupKey)
                )
                .Where(group => group > 0)
                .ToHashSet();

            var toggleGroup = GetComponentInParent<ToggleGroup>();
            foreach (var group in toggleGroups)
            {
                toggleGroup.RegisterReciever(group, Interact);
            }

            isTrapdoor = node.modifications.Any(m => m.Tile.Type == TiledConfiguration.instance.TrapDoorClass);

            managed = config.FirstValue(
                TiledConfiguration.instance.DoorClass, 
                prop => prop == null ? false : prop.Bool(TiledConfiguration.instance.ObjManagedKey, false));

            automaticTrapDoor = config.GetObjectValues(
                TiledConfiguration.instance.TrapDoorClass,
                props => props.Interaction(TiledConfiguration.instance.InteractionKey) == TDEnumInteraction.Automatic
            ).Any();

            _activeTransition = Transition.None;
            isOpen = config.FirstValue(
                TiledConfiguration.instance.ObjInitialClass,
                props => props == null ? false : props.Bool(TiledConfiguration.instance.OpenKey)
            );
            OnDoorChange?.Invoke(this, ActiveTransition, isOpen, null);

            if (triggerAnimations)
            {
                if (isOpen)
                {
                    // Debug.Log(PrefixLogMessage("Animating as opened"));
                    anim.ResetTrigger(ClosedAnimation);
                    anim.SetTrigger(OpenedAnimation);
                } else
                {
                    // Debug.Log(PrefixLogMessage("Animating as closed"));
                    anim.ResetTrigger(OpenedAnimation);
                    anim.SetTrigger(ClosedAnimation);
                }
            }

            isLocked = modifications.Any(
                mod => mod.Tile.CustomProperties.InteractionOrDefault(
                    TiledConfiguration.instance.InteractionKey,
                    TDEnumInteraction.Open) == TDEnumInteraction.Locked
            );

            key = config.FirstValue(
                TiledConfiguration.instance.ObjLockItemClass,
                props => props?.String(TiledConfiguration.instance.KeyKey)
            );

            consumesKey = config.FirstValue(
                TiledConfiguration.instance.ObjLockItemClass,
                props => props == null ? false : props.Bool(TiledConfiguration.instance.ConusumesKeyKey)
            );

            if (interactionDirectionLimitation != Direction.None && !isTrapdoor)
            {
                if (interactionDirectionLimitation.Either(Direction.South, Direction.West))
                {
                    if (KeyReaderNorth != null)
                    {
                        DestroyImmediate(KeyReaderNorth);
                        KeyReaderNorth = null;
                    }
                    if (ReaderImageNorth != null)
                    {
                        DestroyImmediate(ReaderImageNorth.transform.parent.gameObject);
                        ReaderImageNorth = null;
                    }
                } else
                {
                    if (KeyReaderSouth != null)
                    {
                        DestroyImmediate(KeyReaderSouth);
                        KeyReaderSouth = null;
                    }
                    if (ReaderImageSouth)
                    {
                        DestroyImmediate(ReaderImageSouth.transform.parent.gameObject);
                        ReaderImageSouth = null;
                    }
                }
            }

            SyncImage(ReaderImageSouth, isOpen, isLocked);
            SyncImage(ReaderImageNorth, isOpen, isLocked);

            /*
            Debug.Log(PrefixLogMessage(
                $"Synced as Locked({isLocked}) Key({key}; consumes={consumesKey}) Open({isOpen}) Automatic({automaticTrapDoor})"
            ));
            */

            synced = true;
        }

        void SyncImage(Image image, bool opened, bool locked)
        {
            if (image != null)
            {
                if (opened)
                {
                    image.sprite = openedSprite;
                    image.color = openedColor;
                } else if (locked)
                {
                    image.sprite = lockedSprite;
                    image.color = lockedColor;
                } else
                {
                    image.sprite = closedSprite;
                    image.color = closedColor;
                }
            }
        }

        public bool BlocksEntryFrom(Direction direction)
        {
            if (edge != Direction.None)
            {
                return BlockingPassage && edge == direction;
            }

            if (direction.AsAxis() != TraversalAxis) return false;

            return BlockingPassage;
        }

        private void OnLoadGameSave(GameSave save)
        {
            if (save == null)
            {
                return;
            }
            if (!synced) SyncDoor(false);

            var lvl = Dungeon.MapName;

            var doorSave = save.levels[lvl]?.doors?.GetValueOrDefault(StartCoordinates);

            if (doorSave == null)
            {
                Debug.LogError(PrefixLogMessage("I have no saved state"));
                return;
            }

            isOpen = doorSave.isOpen;
            OnDoorChange?.Invoke(this, ActiveTransition, isOpen, null);

            isLocked = doorSave.isLocked;


            if (isOpen)
            {
                // Debug.Log(PrefixLogMessage("Animating as opened"));
                anim.ResetTrigger(ClosedAnimation);
                anim.SetTrigger(OpenedAnimation);
            } else
            {
                // Debug.Log(PrefixLogMessage("Animating as closed"));
                anim.ResetTrigger(OpenedAnimation);
                anim.SetTrigger(ClosedAnimation);
            }

            SyncImage(ReaderImageSouth, isOpen, isLocked);
            SyncImage(ReaderImageNorth, isOpen, isLocked);

            Debug.Log(PrefixLogMessage($"Loaded as isOpen({isOpen}) and isLocked({isLocked})"));
        }

        public KeyValuePair<Vector3Int, DoorSave> Save() => new KeyValuePair<Vector3Int, DoorSave>(
            StartCoordinates,
            new DoorSave(isOpen, isLocked));

        public void OnLoad<T>(T save) where T : new()
        {
            if (save is GameSave)
            {
                OnLoadGameSave(save as GameSave);
            }
        }

        private void Update()
        {
            if (ActiveTransition != Transition.None)
            {
                if (ActiveTransition == Transition.Opening && !isOpen)
                {
                    isOpen = (Time.timeSinceLevelLoad - openingStart) > considerOpenAfterProgress;
                    OnDoorChange?.Invoke(this, ActiveTransition, isOpen, interactingEntity);
                } else
                {
                    var info = anim.GetCurrentAnimatorStateInfo(animLayer);
                    // TODO: This is a hack but something isn't right with normalized time
                    if (info.normalizedTime >= info.length || info.length < 0.25f && !info.loop)
                    {
                        isOpen = _activeTransition == Transition.Opening;
                        _activeTransition = Transition.None;

                        OnDoorChange?.Invoke(this, ActiveTransition, isOpen, interactingEntity);

                        interactingEntity = null;
                    }
                }
            }

            if (activelyMovingEntities.Any(AutomaticTrapdoorAction))
            {
                if (ActiveTransition != Transition.Opening && !isOpen)
                {
                    OpenDoor(null);
                }
            }

        }
    }
}
