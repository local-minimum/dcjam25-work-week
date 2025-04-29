using LMCore.Crawler;
using LMCore.TiledDungeon.DungeonFeatures;
using UnityEngine;

public class MirrorCameraTrigger : TDFeature 
{
    [SerializeField]
    Camera mirrorCamera;

    [SerializeField]
    bool ActivateOnEntry;

    [SerializeField]
    bool ByRegion;

    private void Start()
    {
        if (ByRegion)
        {
            var myRegion = GetComponentInParent<LevelRegion>();
            if (myRegion != null)
            {
                if (LevelRegion.InRegion(myRegion.RegionId, Dungeon.Player.Coordinates))
                {
                    mirrorCamera.enabled = ActivateOnEntry;
                }
            }
        } else if (Dungeon.Player.Coordinates == Coordinates)
        {
            mirrorCamera.enabled = ActivateOnEntry;
        }
    }

    private void OnEnable()
    {
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
        LevelRegion.OnEnterRegion += LevelRegion_OnEnterRegion;
    }

    private void OnDisable()
    {
        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
        LevelRegion.OnEnterRegion -= LevelRegion_OnEnterRegion;
    }

    private void LevelRegion_OnEnterRegion(GridEntity entity, string regionId)
    {
        if (!ByRegion) return;

        var myRegion = GetComponentInParent<LevelRegion>();

        if (myRegion == null) 
        {
            Debug.LogError($"MirrorCameraTrigger {name}: Is activated by region, but not in one themselves");
        } else if (myRegion.RegionId == regionId && entity.EntityType == GridEntityType.PlayerCharacter)
        {
            mirrorCamera.enabled = ActivateOnEntry;
        }
    }

    private void GridEntity_OnPositionTransition(GridEntity entity)
    {
        if (ByRegion) return;

        if (entity.EntityType == GridEntityType.PlayerCharacter && entity.Coordinates == Coordinates)
        {
            mirrorCamera.enabled = ActivateOnEntry; 
        }
    }
}
