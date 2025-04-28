using LMCore.TiledDungeon.Enemies;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{
    public class TDEnemyLoitering : TDAbsEnemyBehaviour
    {
        [SerializeField]
        float checkActivityEvery = 1f;

        float nextCheck;

        private void OnEnable()
        {
            nextCheck = Time.timeSinceLevelLoad + nextCheck;
        }

        private void Update()
        {
            if (Time.timeSinceLevelLoad > nextCheck)
            {
                nextCheck = Time.timeSinceLevelLoad + nextCheck;
                Enemy.UpdateActivity();
            }
        }
    }
}
