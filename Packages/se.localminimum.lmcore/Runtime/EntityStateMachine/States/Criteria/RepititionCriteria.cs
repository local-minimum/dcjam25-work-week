using UnityEngine;

namespace LMCore.EntitySM.State.Critera
{
    public class RepititionCriteria : AbsCustomPassingCriteria
    {
        [SerializeField, Range(0.0001f, 10f)]
        float decreaseOverBySeconds = 0.5f;

        [SerializeField]
        bool increaseResetsTickTime;

        [SerializeField]
        int threshold = 3;

        // TODO: Should possibly in a save state
        int repititions;

        float nextTick;

        public void Increase()
        {
            repititions++;
            Debug.Log($"Repetition Criteria '{name}': Now at {repititions} reps");
            if (repititions == 1 || increaseResetsTickTime) SetNextTick();
        }

        public void Clear()
        {
            repititions = 0;
            Debug.Log($"Repetition Criteria '{name}': Cleared to {repititions} reps");
            if (OneTime)
            {
                enabled = false;
            }
        }

        void SetNextTick()
        {
            nextTick = Time.timeSinceLevelLoad + (1f / decreaseOverBySeconds);
        }

        public override bool Passing => repititions > threshold;

        private void Update()
        {
            if (repititions <= 0 || Time.timeSinceLevelLoad < nextTick) return;

            repititions = Mathf.Max(0, repititions - 1);

            SetNextTick();
        }

        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log($"Repitition Criteria '{name}': {repititions} reps, {Mathf.Max(nextTick - Time.timeSinceLevelLoad, 0)} s until decrease");
        }
    }
}
