using LMCore.Crawler;
using LMCore.TiledDungeon.DungeonFeatures;
using UnityEngine;

public class MirrorCameraTrigger : TDFeature 
{
    [SerializeField]
    Camera mirrorCamera;

    [SerializeField]
    bool ActivateOnEntry;

    private void Start()
    {
        if (!ActivateOnEntry && Dungeon.Player.Coordinates != Coordinates)
        {
            mirrorCamera.enabled = false;
        }
    }

    private void OnEnable()
    {
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
    }

    private void OnDisable()
    {
        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
    }

    private void GridEntity_OnPositionTransition(GridEntity entity)
    {
        if (entity.EntityType == GridEntityType.PlayerCharacter && entity.Coordinates == Coordinates)
        {
            mirrorCamera.enabled = ActivateOnEntry; 
        }
    }
}
