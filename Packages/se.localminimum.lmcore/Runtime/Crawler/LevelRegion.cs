using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Crawler
{
    public delegate void LevelRegionEvent(GridEntity entity, string regionId);

    public class LevelRegion : MonoBehaviour
    {
        public static event LevelRegionEvent OnEnterRegion;
        public static event LevelRegionEvent OnStayRegion;
        public static event LevelRegionEvent OnExitRegion;

        static Dictionary<string, List<LevelRegion>> regions = new Dictionary<string, List<LevelRegion>>();

        [SerializeField, HideInInspector]
        string regionId;
        public string RegionId => regionId;

        List<GridEntity> entitiesHere = new List<GridEntity>();
        List<GridEntity> wasHere = new List<GridEntity>();

        public static bool InRegion(string regionId, GridEntity entity) => 
            regions.ContainsKey(regionId) ? regions[regionId].Any(lr => lr.entitiesHere.Contains(entity)) : false;
        public static bool InRegion(string regionId, Vector3Int coordinates) => 
            regions.ContainsKey(regionId) ? regions[regionId].Any(lr => lr.Coordinates == coordinates) : false;
        public static bool WasInRegion(string regionId, GridEntity entity) => 
            regions.ContainsKey(regionId) ? regions[regionId].Any(lr => lr.wasHere.Contains(entity)) : false;

        public static IEnumerable<GridEntity> Entities(string regionId) =>
            regions.ContainsKey(regionId) ? regions[regionId].SelectMany(lr => lr.entitiesHere) : null;

        IDungeonNode _Node;

        protected Vector3Int Coordinates
        {
            get
            {
                if (_Node == null)
                {
                    _Node = GetComponentInParent<IDungeonNode>();
                }

                if (_Node == null)
                {
                    Debug.LogError($"LevelRegion {name}: Not a child of a dungeon node");
                    return Vector3Int.zero;
                }

                return _Node.Coordinates;
            }
        }

        public void Configure(string regionId)
        {
            this.regionId = regionId;
        }

        private void OnEnable()
        {
            if (regions.ContainsKey(regionId))
            {
                regions[regionId].Add(this);
            } else
            {
                regions.Add(regionId, new List<LevelRegion>() { this });
            }

            GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
        }

        private void OnDisable()
        {
            regions[regionId].Remove(this);
            GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
        }

        private void GridEntity_OnPositionTransition(GridEntity entity)
        {
            bool here = entity.Coordinates == Coordinates;

            if (entitiesHere.Contains(entity))
            {
                if (here)
                {
                    OnStayRegion?.Invoke(entity, regionId);
                } else
                {
                    entitiesHere.Remove(entity);
                    wasHere.Add(entity);
                    
                    if (!InRegion(regionId, entity.Coordinates))
                    {
                        OnExitRegion?.Invoke(entity, regionId);
                    }
                }
            } else if (here)
            {
                // This check must be done before adding entity to here
                bool cameFromSameRegion = InRegion(regionId, entity) || WasInRegion(regionId, entity);
                entitiesHere.Add(entity);
                
                if (cameFromSameRegion)
                {
                    OnStayRegion?.Invoke(entity, regionId);
                } else
                {
                    OnEnterRegion?.Invoke(entity, regionId);
                }
            }
        }

        private void Update()
        {
            wasHere.Clear();
        }

        public void Adopt(GridEntity entity)
        {
            if (wasHere.Contains(entity))
            {
                // Region itself did already think we left so we must regret that
                entitiesHere.Add(entity);
                OnEnterRegion?.Invoke(entity, regionId);
            } else if (entitiesHere.Contains(entity))
            {
                OnStayRegion?.Invoke(entity, regionId);
            } else
            {
                entitiesHere.Add(entity);
                OnEnterRegion?.Invoke(entity, regionId);
            }
        }

        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log($"Region '{regionId}' @ {Coordinates}: Entities here {string.Join(", ", entitiesHere.Select(e => e.Identifier))}");
        }
    }
}
