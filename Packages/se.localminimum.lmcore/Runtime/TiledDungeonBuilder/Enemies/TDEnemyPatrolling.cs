using LMCore.Crawler;
using LMCore.EntitySM.State;
using LMCore.Extensions;
using LMCore.IO;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledDungeon.Enemies;
using LMCore.TiledDungeon.SaveLoad;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDEnemyPatrolling : TDAbsEnemyBehaviour, IOnLoadSave
    {
        [SerializeField]
        bool askPathManagerAtEndOfPath;

        [SerializeField, Tooltip("Note that the turn durations are scaled by Entity abilities")]
        float movementDuration = 2f;

        [SerializeField, Range(0f, 1f)]
        float fallDurationFactor = 0.5f;

        #region SaveState
        bool Patrolling => HasTarget && enabled;
        TDPathCheckpoint target;
        int direction;
        #endregion

        public bool HasTarget => target != null;

        string PrefixLogMessage(string message) =>
            $"Patrolling {Enemy.name} ({Patrolling}, {target}, {direction}): {message}";

        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log(PrefixLogMessage(
                $"Loop (direction {direction}) is {string.Join(" -> ", TDPathCheckpoint.GetLoop(Enemy, target.Loop))}"));
        }

        public void InitOrResumePatrol()
        {
            if (!HasTarget) GetNextCheckpoint();
        }

        private void OnEnable()
        {
            foreach (var perception in GetComponentsInParent<TDEnemyPerception>(true))
            {
                perception.OnDetectPlayer += Perception_OnDetectPlayer;
            }
        }

        private void OnDisable()
        {
            previousPath = null;

            foreach (var perception in GetComponentsInParent<TDEnemyPerception>(true))
            {
                perception.OnDetectPlayer -= Perception_OnDetectPlayer;
            }
        }

        private void Perception_OnDetectPlayer(GridEntity player)
        {
            Enemy.UpdateActivity();
        }

        private void Update()
        {
            if (Paused || !Patrolling) return;

            var entity = Enemy.Entity;
            if (entity.Moving != MovementType.Stationary) return;

            if (entity.Coordinates == target.Coordinates)
            {
                GetNextCheckpoint();
                Enemy.UpdateActivity();
                if (!Patrolling) return;
            }

            if (entity.Falling)
            {
                Enemy.Entity.MovementInterpreter.InvokeMovement(
                    Movement.AbsDown,
                    movementDuration * fallDurationFactor,
                    true);
                return;
            }

            var dungeon = Enemy.Dungeon;
            if (dungeon.ClosestPath(entity, entity.Coordinates, target.Coordinates, Enemy.ArbitraryMaxPathSearchDepth, out var path, refuseSafeZones: true))
            {
                previousPath = path;

                if (path.Count > 1)
                {
                    var translation = path[1];
                    // Debug.Log(path.Debug());
                    if (entity.Coordinates != translation.Checkpoint.Coordinates || translation.TranslationHere != entity.LookDirection)
                    {
                        InvokePathBasedMovement(
                            translation.TranslationHere,
                            target.Coordinates,
                            movementDuration,
                            prefixLogMessage: PrefixLogMessage);
                    }
                }
                else
                {
                    // TODO: Consider better fallback / force getting off patroll
                    Debug.LogWarning(PrefixLogMessage("Didn't find a path to target"));
                    entity.MovementInterpreter.InvokeMovement(
                        Movement.Forward,
                        movementDuration,
                        false);
                    Enemy.UpdateActivity();
                }
            }

            Enemy.MayTaxStay = true;
        }

        void TrySwappingPatrolLoop()
        {
            // We have nothing on current loop, lets see if there's another loop
            var newTarget = Enemy.ClosestCheckpointOnOtherLoop(target);
            if (newTarget != null)
            {
                Debug.Log(PrefixLogMessage($"Swapping loop {target} -> {newTarget}"));
                target = newTarget;
                if (target.Rank == 0)
                {
                    direction = 1;
                }
                else if (target.Rank == Enemy.LoopMaxRank(target.Loop))
                {
                    direction = -1;
                }
            }
            else
            {
                Debug.LogError(PrefixLogMessage($"Didn't find any new target after {target} in direction {direction}"));
                Enemy.UpdateActivity();
            }
        }

        void ResetTargetAndDirection()
        {
            target = null;
            direction = 1;
        }

        void CheckTargetForcesNewState()
        {
            if (target != null && !target.ForceState.Either(StateType.None, StateType.Patrolling))
            {
                // If were mean to do something else lets do that
                Enemy.ForceActivity(target.ForceState, target.ForceStateLookDirection);
            }
        }

        void GetNextCheckpoint()
        {
            if (target == null) {
                TrySwappingPatrolLoop();
                return;
            }

            if (target.Terminal)
            {
                CheckTargetForcesNewState();

                // We've completed our patrol path and it said we should ask ourselves what
                // to do next
                ResetTargetAndDirection();

                Enemy.UpdateActivity(true);
                return;
            }

            CheckTargetForcesNewState();

            // We ready next checkpoint no matter if we're doing other thing inbetween

            var options = Enemy.GetNextCheckpoints(target, direction, out int newDirection);
            if (options != null)
            {
                // There's some new checkpoint
                var newTarget = options.FirstOrDefault(t => t != target);
                if (askPathManagerAtEndOfPath && (newTarget == null || newTarget.Rank < target.Rank))
                {
                    AskPathManagerForTarget(newTarget, newDirection);
                } else if (newTarget == null)
                {
                    TrySwappingPatrolLoop();
                }
                else
                {
                    // Debug.Log(PrefixLogMessage($"Swapping {target} -> {newTarget} and direction {direction} -> {newDirection}"));
                    direction = newDirection;
                    target = newTarget;
                }
            }
            else if (askPathManagerAtEndOfPath)
            {
                AskPathManagerForTarget(null, direction);
            }
            else
            {
                TrySwappingPatrolLoop();
            }
        }

        void AskPathManagerForTarget(TDPathCheckpoint fallback, int fallbackDirection)
        {
            var manager = GetComponentInChildren<TDEnemyPathManager>();
            if (manager != null)
            {
                target = manager.GetNextTarget(target.Loop);
                direction = 1;
            }

            if (target == null)
            {
                target = fallback;
                direction = fallbackDirection;
            }

            if (target == null) TrySwappingPatrolLoop();
        }

        public EnemyPatrollingSave Save() =>
                new EnemyPatrollingSave()
                {
                    active = Patrolling,
                    direction = direction,
                    loop = target?.Loop ?? 0,
                    rank = target?.Rank ?? 0,
                };

        #region Save/Load
        public int OnLoadPriority => 500;
        private void OnLoadGameSave(GameSave save)
        {
            if (save == null)
            {
                return;
            }

            var lvl = Dungeon.MapName;

            var lvlSave = save.levels[lvl];
            if (lvlSave == null)
            {
                return;
            }

            var patrollingSave = lvlSave.enemies.FirstOrDefault(s => s.Id == Enemy.Id && s.Alive)?.patrolling;
            if (patrollingSave != null)
            {
                direction = patrollingSave.direction;
                target = TDPathCheckpoint.GetAll(Enemy, patrollingSave.loop, patrollingSave.rank).FirstOrDefault();

                if (target == null)
                {
                    Debug.LogError(PrefixLogMessage($"Could not find target (loop {patrollingSave.loop}, rank {patrollingSave.rank})"));
                }
                else
                {
                    enabled = patrollingSave.active;
                }
            }
            else
            {
                enabled = false;
            }
        }

        public void OnLoad<T>(T save) where T : new()
        {
            if (save is GameSave)
            {
                OnLoadGameSave(save as GameSave);
            }
        }
        #endregion
    }
}
