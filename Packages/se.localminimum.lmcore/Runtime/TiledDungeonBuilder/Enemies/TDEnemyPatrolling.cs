using LMCore.Crawler;
using LMCore.EntitySM.State;
using LMCore.Extensions;
using LMCore.IO;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledDungeon.SaveLoad;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{
    public class TDEnemyPatrolling : TDAbsEnemyBehaviour, IOnLoadSave
    {
        [SerializeField]
        bool askPathManagerAtEndOfPath;

        [SerializeField, Tooltip("Note that the turn durations are scaled by Entity abilities")]
        float movementDuration = 2f;

        [SerializeField, Range(0f, 1f)]
        float fallDurationFactor = 0.5f;

        [SerializeField]
        float checkActivityEvery = 1.5f;

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
            if (target == null)
            {
                Debug.LogWarning(PrefixLogMessage("Patrol has no target"));
            } else
            {

                Debug.Log(PrefixLogMessage(
                    $"Loop (direction {direction}) is {string.Join(" -> ", TDPathCheckpoint.GetLoop(Enemy, target.Loop))}"));
            }
        }

        public void InitOrResumePatrol()
        {
            if (!HasTarget) SetNextCheckpoint();
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
            nextUpdateActivity = Time.timeSinceLevelLoad + checkActivityEvery;
        }

        private void Update()
        {
            if (Paused || !Patrolling) return;

            var entity = Enemy.Entity;
            if (entity.Moving != MovementType.Stationary) return;

            if (Time.timeSinceLevelLoad > nextUpdateActivity)
            {
                Enemy.UpdateActivity();
                if (!enabled) return;
            }

            if (entity.Coordinates == target.Coordinates)
            {
                SetNextCheckpoint();
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
                    } else
                    {
                        Debug.LogWarning($"We got a dubious result where my coords {entity.Coordinates} equals {translation.Checkpoint.Coordinates} and " +
                            $"we need to look {translation.TranslationHere} is same as my look direction {entity.LookDirection}");

                        Debug.Log(path.Debug());
                        entity.Coordinates = translation.Checkpoint.Coordinates;
                        entity.LookDirection = translation.TranslationHere;
                        entity.Sync();
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
                    nextUpdateActivity = Time.timeSinceLevelLoad + checkActivityEvery;
                }
            }

            Enemy.MayTaxStay = true;
        }

        float nextUpdateActivity;

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
                nextUpdateActivity = Time.timeSinceLevelLoad + checkActivityEvery;
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

        public void ForceSetCheckpoint(int loop, int rank, int direction)
        {
            var options = Enemy.GetCheckpoints(loop, rank).ToList();
            if (options.Count > 0)
            {
                this.target = options.First(); 
                this.direction = direction;
                Debug.Log(PrefixLogMessage($"Checkpoint updated to {target}"));
            } else
            {
                Debug.LogError(PrefixLogMessage($"There's no loop {loop} rank {rank} checkpoint"));
            }
        }

        public void ForceSetCheckpoint(TDPathCheckpoint target, int direction)
        {
            this.target = target;
            this.direction = direction;
        }

        void SetNextCheckpoint()
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
                var newTarget = manager.GetNextTarget(target.Loop);
                if (newTarget != null)
                {
                    target = newTarget;
                    direction = 1;
                    return;
                }
            }

            if (target == null && fallback != null)
            {
                target = fallback;
                direction = fallbackDirection;
                return;
            }

            TrySwappingPatrolLoop();
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
