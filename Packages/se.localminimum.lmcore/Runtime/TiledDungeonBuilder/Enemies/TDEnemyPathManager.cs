using LMCore.Extensions;
using LMCore.TiledDungeon.DungeonFeatures;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{

    public class TDEnemyPathManager : MonoBehaviour
    {
        [System.Serializable]
        public class Transition
        {
            public int FromLoop;
            public List<int> TargetLoops = new List<int>();
        }

        [SerializeField]
        List<Transition> transitions = new List<Transition>();

        TDEnemy _enemy;
        protected TDEnemy Enemy
        {
            get
            {
                if (_enemy == null)
                {
                    _enemy = GetComponentInParent<TDEnemy>();
                }
                return _enemy;
            }
        }

        public TDPathCheckpoint GetNextTarget(int currentLoop)
        {
            var transition = transitions.FirstOrDefault(t => t.FromLoop == currentLoop);

            if (transition == null) return null;

            var loop = transition.TargetLoops.GetRandomElement();
            return Enemy.GetCheckpoints(loop, 0).FirstOrDefault();
        }
    }
}
