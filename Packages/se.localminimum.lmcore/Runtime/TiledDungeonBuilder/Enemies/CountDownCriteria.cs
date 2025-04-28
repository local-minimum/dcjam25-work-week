using LMCore.EntitySM.State.Critera;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{
    public class CountDownCriteria : AbsCustomPassingCriteria 
    {
        [SerializeField]
        float minDuration = 3f;

        [SerializeField]
        float maxDuration = 3f;

        float duration;
        float t0;

        bool passingChecked;
        public override bool Passing {
            get
            {
                var passing = (Time.timeSinceLevelLoad - t0) > duration;
                if (OneTime && passing)
                {
                    Debug.Log($"DurationCriteria {name}: Onetime passing check done");
                    passingChecked = true;
                }
                return passing;
            }
        }

        private void OnEnable()
        {
            RestoreChecks();
        }

        public void Restore()
        {
            RestoreChecks();
            enabled = true;
        }

        void RestoreChecks()
        {
            t0 = Time.timeSinceLevelLoad;
            duration = Random.Range(minDuration, maxDuration);
            passingChecked = false;
        }

        private void Update()
        {
            if (OneTime && passingChecked)
            {
                passingChecked = false;
                enabled = false;
            }
        }
    }
}
