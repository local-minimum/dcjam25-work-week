using LMCore.AbstractClasses;
using LMCore.Crawler;
using LMCore.IO;
using LMCore.TiledDungeon;
using LMCore.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public delegate void ManagerNotGroggyEvent();

public class BossBattleManager : Singleton<BossBattleManager, BossBattleManager>, IOnLoadSave
{
    public static BossBattleManager SafeInstance =>
        InstanceOrResource("BossBattleManager");

    public static event ManagerNotGroggyEvent OnMangerNotGroggy;

    [SerializeField]
    int maxDifficulty = 5;

    [SerializeField]
    int managerGroggyAfterLossSteps = 10;

    [SerializeField]
    Crossfader crossfader;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
        TDLevelManager.OnSceneLoaded += TDLevelManager_OnSceneLoaded;
    }

    private void OnDisable()
    {
        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
        TDLevelManager.OnSceneLoaded -= TDLevelManager_OnSceneLoaded;
    }

    private void TDLevelManager_OnSceneLoaded(string sceneName)
    {
        if (BBFight.FightStatus == FightStatus.InProgress)
        {
            LoadBossFight();
        }
    }


    private void GridEntity_OnPositionTransition(GridEntity entity)
    {

        if (ManagerGroggySteps > 0)
        {
            if (entity.EntityType == GridEntityType.PlayerCharacter)
            {
                ManagerGroggySteps--;
                if (ManagerGroggySteps == 0)
                {
                    OnMangerNotGroggy?.Invoke();
                }
            }
            return;
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
        Debug.Log($"BBManager: Groggy {GroggyBoss} for {ManagerGroggySteps} next difficulty {BattleDifficulty} in battle {BattleTriggered}");
    }

    #region Save / Load

    public bool BattleTriggered { get; private set; }
    public int BattleDifficulty { get; private set; } = 1; 

    int ManagerGroggySteps { get; set; }
    public bool GroggyBoss => ManagerGroggySteps > 0;

    public BossBattleSave Save() =>
        new BossBattleSave() { 
            triggered = BattleTriggered, 
            difficulty = BattleDifficulty,
            managerGroggySteps = ManagerGroggySteps,
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
                save.battle.managerGroggySteps = managerGroggyAfterLossSteps;
                save.battle.difficulty = Mathf.Clamp(save.battle.difficulty + 1, 1, maxDifficulty);
                BattleTriggered = false;
                break;
            case FightStatus.Died:
                Debug.Log("BBManager: We've loaded in from a loosing boss encounter");
                if (save.battle == null)
                {
                    save.battle = new BossBattleSave();
                }
                save.battle.managerGroggySteps = 0;
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
            ManagerGroggySteps = save.battle.managerGroggySteps;

        } else
        {
            BattleTriggered = false;
            BattleDifficulty = 1;
            ManagerGroggySteps = 0;
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
