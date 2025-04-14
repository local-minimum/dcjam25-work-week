using LMCore.Crawler;
using LMCore.TiledDungeon;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledImporter;
using UnityEngine;

public enum ExitType { MainExit, FireEscape, AnomalyDeath, BossDeath };

public delegate void ExitOfficeEvent(ExitType exitType);

public class ExitTrigger : TDFeature, ITDCustom
{
    public static event ExitOfficeEvent OnExitOffice;

    [SerializeField, HideInInspector]
    ExitType exitType;

    private void OnEnable()
    {
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
        TiledDungeon.OnDungeonUnload += TiledDungeon_OnDungeonUnload;
    }

    private void OnDisable()
    {
        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
        TiledDungeon.OnDungeonUnload -= TiledDungeon_OnDungeonUnload;
    }

    private void TiledDungeon_OnDungeonUnload(TiledDungeon dungeon)
    {
        capturedPlayer = null;
    }

    GridEntity capturedPlayer;

    private void GridEntity_OnPositionTransition(GridEntity entity)
    {
        if (capturedPlayer) return;

        if (entity.EntityType == GridEntityType.PlayerCharacter && entity.Coordinates == Coordinates)
        {
            entity.MovementBlockers.Add(this);

            // TODO: Some fancy animation
            capturedPlayer = entity;

            OnExitOffice?.Invoke(exitType);
        }
    }

    public void Configure(TDNode node, TiledCustomProperties properties)
    {
        var mainExit = properties.Bool("MainExit");
        exitType = mainExit ? ExitType.MainExit : ExitType.FireEscape;
    }
}
