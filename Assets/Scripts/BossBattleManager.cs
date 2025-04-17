using LMCore.AbstractClasses;
using LMCore.Crawler;
using LMCore.IO;
using UnityEngine;

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

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
    }

    private void OnDisable()
    {
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
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
    public int BattleDifficulty { get; private set; } = 1; 

    int ManagerGroggySteps { get; set; }
    public bool GroggyBoss => ManagerGroggySteps > 0;

    public BossBattleSave Save() =>
        new BossBattleSave() { 
            triggered = BattleTriggered, 
            difficulty = BattleDifficulty,
            managerGroggySteps = ManagerGroggySteps,
        };

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
