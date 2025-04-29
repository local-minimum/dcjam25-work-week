using LMCore.TiledDungeon;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledDungeon.SaveLoad;
using System.Linq;
using UnityEngine;

public class WWSaveSystem : TDSaveSystem<WWSave, WWSaveSystem>
{
    public static WWSaveSystem SafeInstance =>
        InstanceOrResource("SaveSystem");
        
    [SerializeField]
    string OfficeLevelSceneName;

    [SerializeField]
    string LevelManagerResource;

    TDLevelManager levelManager =>
        TDLevelManager.InstanceOrResource(LevelManagerResource);

    public override WWSave saveData { get; protected set; }

    protected override WWSave CreateSaveState(WWSave active)
    {
        GameSave gameSave = TDSaveSystem.CreateGameSave(active);

        var save = new WWSave(gameSave);

        save.anomalies = AnomalyManager.instance.Save();
        save.battle = BossBattleManager.SafeInstance.Save();
        save.visitedRegions = MainExitHinter.instance.Save().ToList();
        save.playerCoordsHistory = FireExitHinter.instance.Save().ToList();

        Debug.Log($"WWSaveSystem: saving anomalies {save.anomalies}");

        return save;
    }

    public void AutoSave()
    {
        Save(
            0,
            () => Debug.Log(PrefixLogMessage("Temp save saved to slot 0")),
            () => Debug.LogError(PrefixLogMessage("Failed to save temp save to slot 0")));
    }
    public bool HasAutoSave => HasSave(0);

    public void DeleteAutoSave() => DeleteSave(0);

    [ContextMenu("Log status")]
    public override void LogStatus()
    {
        base.LogStatus();
    }

    [ContextMenu("Load autosave")]
    public void LoadAutoSave()
    {
        var startLoadingSave = levelManager.LoadSceneAsync();
        if (startLoadingSave == null)
        {
            Debug.LogError(PrefixLogMessage("Failed to start loading"));
            return;
        }

        var player = FindFirstObjectByType<TDPlayerEntity>();
        if (player == null)
        {
            Debug.LogError(PrefixLogMessage("Without a player, I don't know what scene to unload"));
        }

        LoadSaveAsync(
            0,
            (save, loadSave) => startLoadingSave(
                player.gameObject.scene,
                save.player.levelName,
                completeLoading => {
                    loadSave();
                    completeLoading();
                }),
            () => startLoadingSave(player.gameObject.scene, OfficeLevelSceneName, null));
    }

    [ContextMenu("Wipe all saves")]
    void Wipe()
    {
        DeleteAllSaves();
    }

    private new void OnDestroy()
    {
        base.OnDestroy();
        Destroy(gameObject);
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        TDSavingTrigger.OnAutoSave += TDSavingTrigger_OnAutoSave;
        TiledDungeon.OnDungeonLoad += TiledDungeon_OnDungeonLoad;
    }

    private void OnDisable()
    {
        TDSavingTrigger.OnAutoSave -= TDSavingTrigger_OnAutoSave;
        TiledDungeon.OnDungeonLoad -= TiledDungeon_OnDungeonLoad;
    }

    private void TiledDungeon_OnDungeonLoad(TiledDungeon dungeon, bool fromSave)
    {
        /*
        if (!HasSave(0))
        {
            AutoSave();
        }
        */
    }

    private void TDSavingTrigger_OnAutoSave(SaveType saveType)
    {
        AutoSave();
    }
}
