using LMCore.EntitySM.State;
using LMCore.EntitySM.State.Critera;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{
    public class StateDurationCriteria : AbsCustomPassingCriteria
    {
        [SerializeField]
        float minDuration = 3f;

        [SerializeField]
        float maxDuration = 3f;

        float duration;

        public override bool Passing => isActive && state.ActiveDuration > duration;

        ActivityState state;
        bool isActive;

        private void Start()
        {
            state = GetComponentInParent<ActivityState>();
            if (state == null)
            {
                Debug.LogWarning($"Criteria {name} doesn't belong in a state");
            }
        }

        private void Update()
        {
            if (state.isAcitveState)
            {
                if (!isActive)
                {
                    duration = Random.Range(minDuration, maxDuration);
                    isActive = true;
                }
            } else
            {
                isActive = false;
            }
        }
    }
}
