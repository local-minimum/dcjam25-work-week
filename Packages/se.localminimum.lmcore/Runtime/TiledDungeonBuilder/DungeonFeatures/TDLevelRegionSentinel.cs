using LMCore.Crawler;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public class TDLevelRegionSentinel : TDFeature
    {
        [SerializeField]
        TDDecoration doorsLocation;

        IEnumerable<TDDoor> Doors
        {
            get
            {
                if (doorsLocation == null) yield break;
                var node = doorsLocation.GetComponentInParent<TDNode>();
                if (node == null) yield break;
                foreach (var door in node.gameObject.GetComponentsInChildren<TDDoor>())
                {
                    yield return door;
                }
            }
        }

        private void OnEnable()
        {
            foreach (var door in Doors)
            {
                door.OnDoorChange += Door_OnDoorChange;
            }
        }

        private void OnDisable()
        {
            foreach (var door in Doors)
            {
                door.OnDoorChange -= Door_OnDoorChange;
            }
        }

        private void Door_OnDoorChange(TDDoor door, TDDoor.Transition transition, bool isOpen, GridEntity entity)
        {
            if (transition == TDDoor.Transition.None && isOpen && entity != null)
            {
                // Pretend we're in the region
                var region = Node.GetComponent<LevelRegion>();
                region.Adopt(entity);
            }
        }
    }
}
