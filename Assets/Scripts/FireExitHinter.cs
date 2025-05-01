using LMCore.AbstractClasses;
using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FireExitHinter : Singleton<FireExitHinter, FireExitHinter>, IOnLoadSave
{
    [SerializeField]
    AudioSource speaker;

    [SerializeField]
    List<AudioClip> clips = new List<AudioClip>();

    [SerializeField]
    int triggerIfHistoryLessThan = 8;

    List<Vector3Int> playerCoordinatesHistory = new List<Vector3Int>();

    bool nag = true;

    [ContextMenu("Info")]
    void Info()
    {
        Debug.Log($"FireExitHinter: Nag({nag}) History Length({playerCoordinatesHistory.Count}) {string.Join(" -> ", playerCoordinatesHistory)}");
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

    private void GridEntity_OnPositionTransition(GridEntity entity)
    {
        if (entity.EntityType != GridEntityType.PlayerCharacter) return;
        if (playerCoordinatesHistory.Count == 0 || playerCoordinatesHistory.Last() != entity.Coordinates)
        {
            playerCoordinatesHistory.Add(entity.Coordinates);
        }

    }

    private void LevelRegion_OnEnterRegion(GridEntity entity, string regionId)
    {
        if (entity.EntityType != GridEntityType.PlayerCharacter) return;

        var region = GetComponentInParent<LevelRegion>();
        if (region == null)
        {
            Debug.LogError($"FireExitHinter '{name}': I'm not inside a region");
            return;
        }

        if (!SettingsMenu.MonologueHints.Value) return;

        if (nag && region.RegionId == regionId && playerCoordinatesHistory.Count < triggerIfHistoryLessThan)
        {
            if (!speaker.isPlaying)
            {
                var clip = clips.GetRandomElementOrDefault();
                if (clip != null)
                {
                    speaker.PlayOneShot(clip);
                }
                nag = false;
            }
        }
    }

    #region Save / Load
    public IEnumerable<Vector3Int> Save() => playerCoordinatesHistory;

    public int OnLoadPriority => 10;

    void OnLoad(WWSave save)
    {
        playerCoordinatesHistory.Clear();
        playerCoordinatesHistory.AddRange(save.playerCoordsHistory);
    }

    public void OnLoad<T>(T save) where T : new()
    {
        if (save is WWSave)
        {
            OnLoad(save as WWSave);
        }
    }
    #endregion
}
