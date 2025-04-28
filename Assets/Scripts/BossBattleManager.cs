using LMCore.AbstractClasses;
using LMCore.Crawler;
using LMCore.IO;
using LMCore.TiledDungeon;
using LMCore.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public delegate void BossBattleManagerEvent();

public class BossBattleManager : Singleton<BossBattleManager, BossBattleManager>, IOnLoadSave
{
    public static event BossBattleManagerEvent OnGroggyManger;
    public static BossBattleManager SafeInstance =>
        InstanceOrResource("BossBattleManager");

    [SerializeField]
    int maxDifficulty = 5;

    [SerializeField]
    Crossfader crossfader;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        TDLevelManager.OnSceneLoaded += TDLevelManager_OnSceneLoaded;
    }

    private void OnDisable()
    {
        TDLevelManager.OnSceneLoaded -= TDLevelManager_OnSceneLoaded;
    }

    private void TDLevelManager_OnSceneLoaded(string sceneName)
    {
        if (BBFight.FightStatus == FightStatus.InProgress)
        {
            LoadBossFight();
        }
    }

    public void SetBattleStartedAndSave()
    {
        BattleTriggered = true;
        WWSaveSystem.instance.AutoSave();
    }

    public void LoadBossFight()
    {
        Debug.Log($"BBManager: Loading boss fight, current status is {BBFight.FightStatus}");

        if (crossfader == null)
        {
            SwapToBossScene();
        } else
        {
            crossfader.FadeIn(SwapToBossScene, keepUIAfterFaded: true);
        }
    }

    private void SwapToBossScene()
    {
        BBFight.BaseDifficulty = BattleDifficulty;
        SceneManager.LoadScene("BossBattleScene");
    }

    [ContextMenu("Info")]
    void Info()
    {
        Debug.Log($"BBManager: Next difficulty {BattleDifficulty} in battle {BattleTriggered}");
    }

    #region Save / Load

    public bool BattleTriggered { get; private set; }
    public int BattleDifficulty { get; private set; } = 1; 

    public BossBattleSave Save() =>
        new BossBattleSave() { 
            triggered = BattleTriggered, 
            difficulty = BattleDifficulty,
        };

    public int OnLoadPriority => 10000;

    void LoadWWSave(WWSave save)
    {
        switch (BBFight.FightStatus)
        {
            case FightStatus.Survived:
                Debug.Log("BBManager: We've loaded in from a winning boss encounter");
                BBFight.FightStatus = FightStatus.None;
                if (save.battle == null)
                {
                    save.battle = new BossBattleSave();
                }
                save.battle.difficulty = Mathf.Clamp(save.battle.difficulty + 1, 1, maxDifficulty);
                BattleTriggered = false;
                OnGroggyManger?.Invoke();
                break;
            case FightStatus.Died:
                Debug.Log("BBManager: We've loaded in from a loosing boss encounter");
                if (save.battle == null)
                {
                    save.battle = new BossBattleSave();
                }
                save.battle.difficulty = Mathf.Max(save.battle.difficulty - 1, 1);

                BattleTriggered = false;
                BBFight.FightStatus = FightStatus.None;
                AnomalyManager.instance.FailBossBattle();
                return;
        }


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
