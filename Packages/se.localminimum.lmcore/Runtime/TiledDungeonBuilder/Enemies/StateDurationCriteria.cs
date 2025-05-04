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

        bool passingChecked;
        public override bool Passing {
            get
            {
                if (!isActive)
                {
                    Debug.LogWarning($"Asking non active critera {name} if passing");
                    return !OneTime || !passingChecked;
                }
                var passing = state.ActiveDuration > duration;
                if (OneTime && passing)
                {
                    Debug.Log($"DurationCriteria {name}: Onetime passing check done");
                    passingChecked = true;
                }
                return passing;
            }
        }

        // This exists to mirror the CountDownCritera; to manually reuse of one shots
        public void Restore()
        {
            isActive = false;
            passingChecked = false;
            enabled = true;
        }

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
                } else if (OneTime && passingChecked)
                {
                    passingChecked = false;
                    enabled = false;
                    Debug.Log($"DurationCriteria {name}: Used up");
                }
            } else if (isActive)
            {
                isActive = false;
            }
        }
    }
}
