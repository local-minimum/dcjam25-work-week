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

            public override string ToString() => $"<Loop {FromLoop} Targets: {string.Join(", ", TargetLoops)}>";
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

            if (transition == null)
            {
                Debug.LogWarning($"PathManager: {currentLoop} not known");
                return null;
            }

            var options = transition.TargetLoops;
            if (options == null || options.Count == 0)
            {
                Debug.LogWarning($"PathManager: Loop {currentLoop} {transition} has no targets");
                return null;
            }

            var loop = options.GetRandomElement();

            var checkpoints = Enemy.GetCheckpoints(loop, 0);
            if (checkpoints == null)
            {
                Debug.LogWarning($"PathManager: {Enemy.name} find no first checkpoint for loop {loop}");
                return null;
            }

            var checkpoint = checkpoints.FirstOrDefault();
            Debug.Log($"PathManager: Enemy {Enemy.name} selected {checkpoint.name} (loop {loop})");
            return checkpoint;
        }
    }
}
