using LMCore.EntitySM.State.Critera;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{
    [RequireComponent(typeof(TDEnemyAttacking))]
    public class AttackingPossibleCriteria : AbsCustomPassingCriteria
    {
        bool wasPassing;

        public override bool Passing
        {

            get
            {
                var passing = GetComponent<TDEnemyAttacking>().attacks.Any(a => a.Ready);

                if (passing)
                {
                    wasPassing = true;
                } else
                {
                    wasPassing = false;
                }

                return passing;
            }
        }

        private void Update()
        {
            if (wasPassing && OneTime)
            {
                enabled = false;
                wasPassing = false;
            }
        }
    }
}
