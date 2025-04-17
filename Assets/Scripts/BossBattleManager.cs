using LMCore.AbstractClasses;
using LMCore.IO;
using UnityEngine;

public class BossBattleManager : Singleton<BossBattleManager, BossBattleManager>, IOnLoadSave
{
    public static BossBattleManager SafeInstance =>
        InstanceOrResource("BossBattleManager");


    [SerializeField]
    int maxDifficulty = 5;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
    
    public void SetBattleStartedAndSave()
    {
        BattleTriggered = true;
        WWSaveSystem.instance.AutoSave();
    }

    public void ReportWin()
    {
        BattleDifficulty = Mathf.Clamp(BattleDifficulty + 1, 1, maxDifficulty);
        BattleTriggered = false;
        WWSaveSystem.instance.AutoSave();
    }

    public void ReportLoss()
    {
        BattleDifficulty = Mathf.Clamp(BattleDifficulty - 1, 1, maxDifficulty);
        BattleTriggered = false;
        WWSaveSystem.instance.AutoSave();
    }

    #region Save / Load

    public bool BattleTriggered { get; private set; }
    public int BattleDifficulty { get; private set; } 

    public BossBattleSave Save() =>
        new BossBattleSave() { triggered = BattleTriggered, difficulty = BattleDifficulty };

    public int OnLoadPriority => 100;

    void LoadWWSave(WWSave save)
    {
        if (save.battle != null)
        {
            BattleTriggered = save.battle.triggered;
            BattleDifficulty = save.battle.difficulty;
        } else
        {
            BattleTriggered = false;
            BattleDifficulty = 1;
        }
    }

    public void OnLoad<T>(T save) where T : new()
    {
        if (save is WWSave)
        {
            LoadWWSave(save as WWSave);
        }
    }
    #endregion
}
