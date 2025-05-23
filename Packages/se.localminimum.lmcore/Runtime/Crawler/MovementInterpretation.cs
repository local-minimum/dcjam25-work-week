﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Crawler
{
    public class MovementInterpretation
    {
        public float DurationScale { get; set; } = 1.0f;
        public Direction PrimaryDirection { get; set; }
        public bool Forced { get; set; }
        public MovementInterpretationOutcome Outcome { get; set; }
        public List<MovementCheckpointWithTransition> Steps { get; set; } = new List<MovementCheckpointWithTransition>();

        public MovementType Movement
        {
            get
            {
                if (Steps.Count == 0) return MovementType.Stationary;
                var origin = Steps.First();
                var rotating = Steps.Any(s => s.Checkpoint.LookDirection != origin.Checkpoint.LookDirection);
                var translating = Steps.Any(s => s.Checkpoint.Coordinates != origin.Checkpoint.Coordinates ||
                    s.Checkpoint.AnchorDirection != origin.Checkpoint.AnchorDirection);

                return (rotating ? MovementType.Rotating : MovementType.Stationary) |
                    (translating ? MovementType.Translating : MovementType.Stationary);
            }
        }
        public override string ToString()
        {
            return $"<MoveInterpret: Direction {PrimaryDirection}, Forced {Forced}, Outcome {Outcome}, Duration {DurationScale} - {string.Join(", ", Steps.Select(s => $"[{s}]"))}>";
        }

        public MovementCheckpointWithTransition First => Steps[0];
        public MovementCheckpointWithTransition Second => Steps[1];
        public MovementCheckpointWithTransition Last => Steps.Last();
        public MovementCheckpointWithTransition SecondToLast => Steps[Steps.Count - 2];

        public IEnumerable<float> Lengths(IDungeon dungeon)
        {
            var previous = First.Checkpoint.Position(dungeon);
            for (int i = 1, n = Steps.Count; i < n; ++i)
            {
                var current = Steps[i].Checkpoint.Position(dungeon);
                yield return (current - previous).magnitude;
                previous = current;
            }
        }

        public float Length(IDungeon dungeon) => Lengths(dungeon).Sum();

        public IEnumerable<float> RelativeSegmentLengths(IDungeon dungeon)
        {
            if (Steps.Count <= 2)
            {
                yield return 1f;
                yield break;
            }
            var total = Length(dungeon);
            if (total == 0f)
            {
                total = 1f;
            }
            foreach (var l in Lengths(dungeon))
            {
                yield return l / total;
            }
        }

        float SteppingProgress(float progress, int stepsPerTransition, System.Func<float, float> easing)
        {
            float stepLength = 1f / stepsPerTransition;
            var remainder = progress % stepLength;

            return progress - remainder + (easing(remainder / stepLength) * stepLength);
        }

        float SteppingProgress(float progress, int stepsPerTransition) =>
            SteppingProgress(progress, stepsPerTransition, (p) => Mathf.SmoothStep(0, 1, p));

        /// <summary>
        /// Lerps position through a jump segment.
        /// </summary>
        /// <param name="start">Jump start</param>
        /// <param name="end">Jump end</param>
        /// <param name="down">Direction of down</param>
        /// <param name="height">Jump height relative to start</param>
        /// <param name="progress">Linear progress through the jump segment (0 - 1)</param>
        /// <returns></returns>
        Vector3 LerpJump(Vector3 start, Vector3 end, Vector3 down, float height, float progress)
        {
            var up = down * -1f;
            return Vector3.Lerp(start, end, progress) + (up * Mathf.Sin(progress * Mathf.PI) * height);
        }

        /// <summary>
        /// Lerps position through stairs segment, matching half a tile
        /// </summary>
        /// <param name="start">Start position of stairs</param>
        /// <param name="end">End position of stairs</param>
        /// <param name="down">Direction of down in stairs</param>
        /// <param name="progress">Linear progress through the stairs segment (0 - 1)</param>
        /// <param name="steps">Number of steps in a full tile</param>
        /// <param name="entering">If it's walking into a tile</param>
        /// <returns></returns>
        Vector3 LerpStairs(Vector3 start, Vector3 end, Direction downDirection, float progress, int steps, bool entering)
        {
            var down = downDirection.AsLookVector3D();
            var delta = end - start;
            var orthogonal = Vector3.Project(delta, down);
            var planar = delta - orthogonal;

            System.Func<float, float> orthoEasing = Vector3.Dot(orthogonal, down) > 0 ? EaseInExpo : EaseOutExpo;
            var adjustedProgress = entering ? progress / 2f : 0.5f + (progress / 2f);
            var adjustedStart = entering ? start : start - delta;
            var adjustedEnd = entering ? end + delta : start;

            return adjustedStart + (2f * (
                (planar * SteppingProgress(adjustedProgress, steps)) +
                (orthogonal * SteppingProgress(adjustedProgress, steps, orthoEasing))));
        }

        /// <summary>
        /// Lerps positions through simple segment, matching half a tile
        /// </summary>
        /// <param name="start">Start position of segment</param>
        /// <param name="end">End position of segment</param>
        /// <param name="progress">Linear progress throug segment (0 - 1)</param>
        /// <param name="easing">Function that eases a tile progress to a position</param>
        /// <param name="entering">If it's walking into a tile</param>
        /// <returns></returns>
        Vector3 LerpHalfNode(Vector3 start, Vector3 end, float progress, System.Func<float, float> easing, bool entering)
        {
            // Delta is half of the total distance.
            var delta = end - start;

            // Progress along the cube face 
            var adjustedProgress = entering ? progress / 2f : 0.5f + (progress / 2f);
            // Start of the cube face
            var adjustedStart = entering ? start : start - delta;

            return adjustedStart + (2f * delta * easing(adjustedProgress));
        }

        /// <summary>
        /// Lerps positions through simple segment, matching half a tile
        /// </summary>
        /// <param name="start">Start position of segment</param>
        /// <param name="end">End position of segment</param>
        /// <param name="progress">Linear progress throug segment (0 - 1)</param>
        /// <param name="steps">Number of steps in a full tile</param>
        /// <param name="entering">If it's walking into a tile</param>
        /// <returns></returns>
        Vector3 LerpHalfNode(Vector3 start, Vector3 end, float progress, int steps, bool entering)
        {
            var delta = end - start;

            var adjustedProgress = entering ? progress / 2f : 0.5f + (progress / 2f);
            var adjustedStart = entering ? start : start - delta;

            return adjustedStart + (2f * delta * SteppingProgress(adjustedProgress, steps));
        }

        bool CalculateMidpoint(GridEntity entity, out Vector3 position, out MovementTransition transition)
        {
            var count = Steps.Count;
            if (count == 2)
            {
                position = Vector3.Lerp(First.Checkpoint.Position(entity.Dungeon), Last.Checkpoint.Position(entity.Dungeon), 0.5f);
                transition = First.Transition;
                return true;
            }
            if (count == 3)
            {
                position = Steps[1].Checkpoint.Position(entity.Dungeon);
                transition = Steps[1].Transition;
                return true;
            }

            if (count != 4)
            {
                position = Vector3.zero;
                transition = MovementTransition.None;
                return false;
            }

            var origin = First;
            var first = Steps[1];
            var second = Steps[2];

            var originPos = origin.Checkpoint.Position(entity.Dungeon);
            var firstPos = first.Checkpoint.Position(entity.Dungeon);
            var secondPos = second.Checkpoint.Position(entity.Dungeon);

            var up = origin.Checkpoint.Down.AsLookVector3D();
            var dUp = Vector3.Project(secondPos - firstPos, up).magnitude;
            if (dUp > entity.Abilities.minScaleHeight)
            {
                position = Vector3.zero;
                transition = MovementTransition.None;
                return false;
            }

            var direction = firstPos - originPos;
            var dFirst = direction.magnitude;
            var norm = direction.normalized;
            var dSecond = Vector3.Project(secondPos - originPos, norm).magnitude;

            if (dSecond < dFirst)
            {
                position = originPos + (norm * dSecond);
                transition = Steps[2].Transition;
                return true;
            }

            if ((dSecond - dFirst) < entity.Abilities.minForwardJump)
            {
                position = Vector3.Lerp(firstPos, secondPos, 0.5f);
                transition = Steps[2].Transition;
                return true;
            }



            position = Vector3.zero;
            transition = MovementTransition.None;
            return false;
        }

        Vector3 EvaluateSegment(
            MovementTransition transition,
            AnchorTraversal traversal,
            Vector3 start,
            Vector3 end,
            Direction down,
            GridEntity entity,
            float segmentProgress,
            bool entering
        )
        {
            if (transition == MovementTransition.Jump)
            {
                var jumpDistance = Vector3.Distance(start, end);

                return LerpJump(
                    start,
                    end,
                    down.AsLookVector3D(),
                    entity.Abilities.jumpHeight * Mathf.Clamp01(jumpDistance / entity.Abilities.maxForwardJump),
                    segmentProgress);
            }

            switch (traversal)
            {
                case AnchorTraversal.None:
                    System.Func<float, float> easing = entity.Falling ? Linear : SmothStep;

                    return LerpHalfNode(start, end, segmentProgress, easing, entering);

                case AnchorTraversal.Walk:
                    return LerpHalfNode(start, end, segmentProgress, entity.Abilities.walkingStepsPerTransition, entering);

                case AnchorTraversal.Conveyor:
                case AnchorTraversal.ConveyorSqueeze:
                    return LerpHalfNode(start, end, segmentProgress, Linear, entering);

                case AnchorTraversal.Climb:
                    return LerpHalfNode(start, end, segmentProgress, entity.Abilities.climbingStepsPerTransition, entering);

                case AnchorTraversal.Scale:
                    // This is for climbing partially elevated things like ramps during the intermediary segment
                    // from side so 2 steps for "full tile" makes it one step
                    return LerpHalfNode(start, end, segmentProgress, 2, entering);

                case AnchorTraversal.Stairs:
                    return LerpStairs(start, end, down, segmentProgress, entity.Abilities.walkingStepsPerTransition, entering);
            }

            Debug.LogError($"Segment not understood with transition {transition} and traversal {traversal}");
            return start;
        }

        #region Ease functions
        // TODO: Test or at least visualize this
        float CubicBezier(float a, float b, float c, float d, float progress)
        {
            float t = Mathf.Clamp01(progress);
            return (a * Mathf.Pow(1 - t, 3)) + (3 * b * Mathf.Pow(1 - t, 2) * t) + (3 * c * (1 - t) * Mathf.Pow(t, 2)) + (d * Mathf.Pow(t, 3));
        }

        // Use for stepups in stairs?
        float EaseOutExpo(float progress)
        {
            float t = Mathf.Clamp01(progress);
            return t == 1 ? 1 : 1 - Mathf.Pow(2, -10 * t);
        }

        float EaseInExpo(float progress)
        {
            var t = Mathf.Clamp01(progress);
            return t == 0 ? 0 : Mathf.Pow(2, (10 * t) - 10);
        }

        float EaseOutCubic(float progress) => 1f - Mathf.Pow(1f - Mathf.Clamp01(progress), 3f);

        float Linear(float progress) => progress;

        float SmothStep(float progress) => Mathf.SmoothStep(0, 1, progress);
        #endregion

        private enum Segment { First, Intermediary, Last }
        private Segment CurrentSegment = Segment.First;
        float segmentStartProgress = 0;

        public IDungeonNode CurrentNode
        {
            get
            {
                if (CurrentSegment == Segment.First)
                {
                    return First.Checkpoint.Node;
                }
                else
                {
                    return Last.Checkpoint.Node;
                }
            }
        }

        void RegretMovementDynamically()
        {
            var start = First;
            MovementCheckpointWithTransition intermediary = new MovementCheckpointWithTransition()
            {
                Checkpoint = MovementCheckpoint.From(
                    start.Checkpoint,
                    PrimaryDirection,
                    start.Checkpoint.LookDirection),
                Transition = First.Transition,
            };

            Steps.Clear();
            Steps.Add(start);
            Steps.Add(intermediary);
            Steps.Add(start);

            Debug.LogWarning("Movement regretted");
            Outcome = MovementInterpretationOutcome.DynamicBounce;
            CurrentSegment = Segment.Last;
        }

        float UnclampedSegmentProgress(GridEntity entity, float progress, int segmentIdx)
        {
            var lengths = RelativeSegmentLengths(entity.Dungeon).ToList();
            if (lengths.Count <= segmentIdx)
            {
                Debug.LogError($"Requesting segment {segmentIdx} is not possible on {this}");
                return 1f;
            }
            var segmentLength = lengths[segmentIdx];
            if (segmentLength == 0) return 1f;

            var remainingProgressAtSegmentStart = 1 - segmentStartProgress;
            var progressOnRemainder = Mathf.Clamp01(progress - segmentStartProgress) / remainingProgressAtSegmentStart;
            var totalRemainingSegmentLengths = lengths.Skip(segmentIdx).Sum();
            var segmentPartOfTotalRemaining = segmentLength / totalRemainingSegmentLengths;
            var segmentProgress = Mathf.Clamp01(progressOnRemainder / segmentPartOfTotalRemaining);

            // Debug.Log($"{segmentIdx} - progress({progress}) progressonremain({progressOnRemainder})  remaining@start({remainingProgressAtSegmentStart}) segmentPartOfTotalRem({segmentPartOfTotalRemaining}) => {segmentProgress}");
            return segmentProgress;
        }

        private void ValidateAndFinalizeMidpointTransition(GridEntity entity)
        {
            var startCoords = First.Checkpoint.Coordinates;

            if (!(Last.Checkpoint.Node?.MayInhabit(entity, PrimaryDirection, Forced) ?? false))
            {
                RegretMovementDynamically();
                return;
            }

            if (Steps.Count != 4) return;

            var midStart = Steps[1];
            var midEnd = Steps[2];
            var delta = midEnd.Checkpoint.Position(entity.Dungeon) -
            midStart.Checkpoint.Position(entity.Dungeon);

            var up = midStart.Checkpoint.Down.Inverse().AsLookVector3D();
            if (midStart.Transition == MovementTransition.Jump)
            {
                var forward = PrimaryDirection.AsLookVector3D();
                if (Vector3.Project(delta, forward).magnitude > entity.Abilities.maxForwardJump)
                {
                    RegretMovementDynamically();
                }
            }
            else if (midStart.Transition == MovementTransition.Grounded)
            {
                if (Vector3.Dot(delta, up) > entity.Abilities.maxScaleHeight)
                {
                    Debug.LogWarning($"Can't move 'up': {Vector3.Dot(delta, up)} > {entity.Abilities.maxScaleHeight}");
                    RegretMovementDynamically();
                }
            }
        }

        /// <summary>
        /// Finalizes the evaluation making sure we are allowed to get to last.
        /// </summary>
        /// <param name="entity"></param>
        public void Evaluate(GridEntity entity)
        {
            ValidateAndFinalizeMidpointTransition(entity);
        }


        public Vector3 Evaluate(GridEntity entity, float progress, out Quaternion rotation, out MovementCheckpoint checkpoint, out float stepProgress)
        {
            // 1. Figure out segment active from total length and progressNumber of steps in a full tile
            //    Take into account freeze frames and step-up/downs and jumps for misaligned tiles
            // 2. If embarking on the transition between first and second path check if valid and
            //    if not evolve self to refused movement.
            // 3. Given transition of segment scale progress to internal 0-1 progress of segment
            //    and lerp its positions using potentially ajusted intermediaries

            if (Outcome == MovementInterpretationOutcome.Bouncing || Outcome == MovementInterpretationOutcome.DynamicBounce)
            {
                if (Outcome == MovementInterpretationOutcome.Bouncing && Steps.Count == 3)
                {
                    var progressCap = entity.LookDirection == PrimaryDirection.Inverse() ?
                        entity.Abilities.refusedMidpointReversingMaxInterpolation :
                        entity.Abilities.refusedMidpointMaxInterpolation;

                    if (progress < 0.5f)
                    {
                        progress = Mathf.Min(progress, 0.5f * progressCap);
                    }
                    else
                    {
                        progress = Mathf.Max(progress, 1 - (0.5f * progressCap));
                    }
                }
            }
            else if (Outcome == MovementInterpretationOutcome.Landing)
            {
                progress = EaseOutCubic(progress);
            }

            var start = First;
            var end = Last;

            if (Steps.Count == 2 && start.Transition == MovementTransition.Jump)
            {
                var startRotation = start.Checkpoint.Rotation(entity);
                var endRotation = end.Checkpoint.Rotation(entity);

                rotation = Quaternion.Lerp(startRotation, endRotation, progress);

                checkpoint = progress < 0.5f ? start.Checkpoint : end.Checkpoint;
                stepProgress = progress;

                return EvaluateSegment(
                    MovementTransition.Jump,
                    AnchorTraversal.None,
                    start.Checkpoint.Position(entity.Dungeon),
                    end.Checkpoint.Position(entity.Dungeon),
                    start.Checkpoint.Down,
                    entity,
                    progress,
                    true); // It doesn't matter for true false here
            }
            else if (CalculateMidpoint(entity, out var mid, out var transition))
            {
                var startRotation = start.Checkpoint.Rotation(entity);
                var endRotation = end.Checkpoint.Rotation(entity);

                rotation = Quaternion.Lerp(startRotation, endRotation, progress);

                if (progress < 0.5f && CurrentSegment == Segment.First)
                {
                    var startPos = start.Checkpoint.Position(entity.Dungeon);
                    checkpoint = start.Checkpoint;
                    stepProgress = progress / 0.5f;

                    return EvaluateSegment(
                        start.Transition,
                        start.Checkpoint.Traversal,
                        startPos,
                        mid,
                        start.Checkpoint.Down,
                        entity,
                        stepProgress,
                        false
                    );
                }
                else
                {
                    if (CurrentSegment != Segment.Last &&
                        !(end.Checkpoint.Node?.MayInhabit(entity, PrimaryDirection, Forced) ?? false))
                    {
                        RegretMovementDynamically();
                    }
                    CurrentSegment = Segment.Last;
                    var endPos = end.Checkpoint.Position(entity.Dungeon);
                    checkpoint = end.Checkpoint;
                    stepProgress = (progress - 0.5f) / 0.5f;

                    return EvaluateSegment(
                        transition,
                        end.Checkpoint.Traversal,
                        mid,
                        endPos,
                        end.Checkpoint.Down,
                        entity,
                        stepProgress,
                        true);
                }
            }
            else
            {
                // Some multistep procedure
                // TODO figure out if it uses the wrong checkpoint with three steps also use relative lengths directly
                var lengths = RelativeSegmentLengths(entity.Dungeon).ToList();
                var segments = lengths.Count;

                if (segments <= 2)
                {
                    checkpoint = progress < 0.5f ? start.Checkpoint : end.Checkpoint;
                    Debug.LogError($"{entity.name} is attempting a {segments} part transition, we don't know how to handle that");

                    var startRotation = start.Checkpoint.Rotation(entity);
                    var endRotation = end.Checkpoint.Rotation(entity);

                    rotation = Quaternion.Lerp(startRotation, endRotation, progress);
                    stepProgress = progress;

                    return Vector3.Lerp(
                        start.Checkpoint.Position(entity.Dungeon),
                        end.Checkpoint.Position(entity.Dungeon),
                        progress
                    );
                }

                var unclampedSegmentProgress = UnclampedSegmentProgress(entity, progress, (int)CurrentSegment);
                // Adjust active segment if we're overshooting the current one 
                var candidateSegmentIdx = Mathf.Min(
                    Mathf.Max(
                        unclampedSegmentProgress >= 1.0f ? (int)CurrentSegment + 1 : 0,
                        (int)CurrentSegment),
                    2);

                // Check if we should progress from first segment / that is commit to the entire movement
                if (candidateSegmentIdx > 0 && CurrentSegment == Segment.First)
                {
                    ValidateAndFinalizeMidpointTransition(entity);
                }


                // perhaps best to recheck where we are after potential updates
                var segmentIdx = Mathf.Min(
                    Mathf.Max(candidateSegmentIdx, (int)CurrentSegment),
                    Steps.Count - 2);

                var segment = (Segment)segmentIdx;

                var pt1 = Steps[segmentIdx];
                var pt2 = Steps[segmentIdx + 1];
                var pt1Rotation = pt1.Checkpoint.Rotation(entity);

                if (CurrentSegment != segment)
                {
                    // TODO: Could subtract overshoot from previous unlcamped progress...
                    segmentStartProgress = progress;
                    CurrentSegment = segment;

                    var delta = pt2.Checkpoint.Position(entity.Dungeon) - pt1.Checkpoint.Position(entity.Dungeon);
                    var midStart = Steps[1];
                    var up = midStart.Checkpoint.Down.Inverse().AsLookVector3D();
                    var climb = Vector3.Dot(delta, up);
                    // Check if we can't step down from this elevation, we must jump
                    if (CurrentSegment == Segment.Intermediary && pt1.Transition == MovementTransition.Grounded && climb < -entity.Abilities.maxScaleHeight)
                    {
                        pt1.Transition = MovementTransition.Jump;
                        var last = Last;
                        if (last.Checkpoint.Node != null && last.Checkpoint.AnchorDirection == Direction.Down)
                        {
                            pt2 = new MovementCheckpointWithTransition()
                            {
                                Checkpoint = MovementCheckpoint.From(last.Checkpoint.Node, Direction.None, pt2.Checkpoint.LookDirection),
                                Transition = MovementTransition.Ungrounded,
                            };
                            Steps[segmentIdx + 1] = pt2;
                        }
                        else
                        {
                            pt2 = Last;
                            Steps.RemoveAt(segmentIdx + 1);
                        }

                        lengths = RelativeSegmentLengths(entity.Dungeon).ToList();
                        Debug.Log($"Dynamically updated interpretation because vertical fall of {climb} to: {this}");
                        Debug.Log($"Segment relative lengths: {string.Join(", ", lengths)}");
                    }
                }

                stepProgress = Mathf.Clamp01(UnclampedSegmentProgress(entity, progress, segmentIdx));

                checkpoint = stepProgress < 0.5f ? pt1.Checkpoint : pt2.Checkpoint;

                var pt2Rotation = pt2.Checkpoint.Rotation(entity);

                rotation = Quaternion.Lerp(pt1Rotation, pt2Rotation, progress);

                return EvaluateSegment(
                    pt1.Transition,
                    pt1.Checkpoint.Traversal,
                    pt1.Checkpoint.Position(entity.Dungeon),
                    pt2.Checkpoint.Position(entity.Dungeon),
                    pt1.Checkpoint.Down,
                    entity,
                    stepProgress,
                    CurrentSegment != Segment.First);
            }
        }
    }
}
