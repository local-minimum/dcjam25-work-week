using LMCore.AbstractClasses;
using LMCore.Extensions;
using LMCore.IO;
using LMCore.TiledDungeon;
using LMCore.UI;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

public delegate void SetAnomalyEvent(string id);
public delegate void SetDayEvent(Weekday day);

public class AnomalyManager : Singleton<AnomalyManager, AnomalyManager>, IOnLoadSave
{
    public static event SetAnomalyEvent OnSetAnomaly;
    public static event SetDayEvent OnSetDay;

    [System.Serializable]
    public enum AnomalyType { NormalOffice, Anomaly, ScaryAnomaly }

    [System.Serializable]
    struct AnomalyPlan
    {
        public int minWeek;
        public int maxWeek;

        public AnomalyType Monday;
        public AnomalyType Tueday;
        public AnomalyType Wednesday;
        public AnomalyType Thursday;
        public AnomalyType Friday;
        public AnomalyType Saturday;
        public AnomalyType Sunday;

        public bool AppliesToWeek(int week) => 
            minWeek <= week && week <= maxWeek;

        public AnomalyType GetPlan(Weekday day)
        {
            switch (day)
            {
                case Weekday.Monday:
                    return Monday;
                case Weekday.Tuesday:
                    return Tueday;
                case Weekday.Wednesday:
                    return Wednesday;
                case Weekday.Thursday:
                    return Thursday;
                case Weekday.Friday:
                    return Friday;
                case Weekday.Saturday:
                    return Saturday;
                case Weekday.Sunday:
                    return Sunday;
                default:
                    return AnomalyType.NormalOffice;
            }
        }

        public AnomalyPlan(List<AnomalyType> plan, int minWeek = 0, int maxWeek = 99)
        {
            this.minWeek = minWeek;
            this.maxWeek = maxWeek;
            Monday = plan.GetNthOrDefault(0, AnomalyType.NormalOffice);
            Tueday = plan.GetNthOrDefault(1, AnomalyType.Anomaly);
            Wednesday = plan.GetNthOrDefault(2, AnomalyType.ScaryAnomaly);
            Thursday = plan.GetNthOrDefault(3, AnomalyType.NormalOffice);
            Friday = plan.GetNthOrDefault(4, AnomalyType.Anomaly);
            Saturday = plan.GetNthOrDefault(5, AnomalyType.Anomaly);
            Sunday = plan.GetNthOrDefault(6, AnomalyType.NormalOffice);
        }

        public static AnomalyPlan RandomizedPlan(int anomalies = 4, int scary = 1)
        {
            var plan = new List<AnomalyType>();
            for (int i = 0; i<Mathf.Min(anomalies, 7); i++)
            {
                plan.Add(i < scary ? AnomalyType.ScaryAnomaly : AnomalyType.Anomaly);
            }

            for (int i = plan.Count; i < 7; i++)
            {
                plan.Add(AnomalyType.NormalOffice);
            }

            Debug.LogWarning("AnomaliesManager: Random week plan created");
            return new AnomalyPlan(plan.Shuffle().ToList());
        }

        public List<AnomalyType> Serialized() =>
            new List<AnomalyType>() { Monday, Tueday, Wednesday, Thursday, Friday, Saturday, Sunday };

        public override string ToString() =>
            $"<Plan Mo:{Monday} Tu:{Tueday} We:{Wednesday} Th:{Thursday} Fr:{Friday} Sa:{Saturday} Su:{Sunday}>";
    }

    bool anomalyLoaded;

    [SerializeField]
    List<AnomalySetting> anomalies = new List<AnomalySetting>();

    [SerializeField]
    Crossfader crossfader;

    [SerializeField, Header("Selection")]
    int selectFromFirstNCandidates = 3;

    [SerializeField]
    List<AnomalyPlan> weekPlans = new List<AnomalyPlan>();

    [System.Serializable]
    struct DifficultyAdjustment
    {
        [Range(0, 10)]
        public int Success;
        [Range(0, 10)]
        public int Fail;
    }

    [Header("Difficulties"), SerializeField, Range(1, 10)]
    int clearDifficultyBase = 1;

    [SerializeField]
    DifficultyAdjustment clearAdjustments = new DifficultyAdjustment() { Success = 1, Fail = 2 };

    [SerializeField, Range(1, 10)]
    int clearDifficultyMax = 5;

    [SerializeField, Range(1, 10)]
    int balancedDifficultyBase = 4;

    [SerializeField]
    DifficultyAdjustment balancedAdjustment = new DifficultyAdjustment() { Success = 2, Fail = 2 };

    [SerializeField, Range(1, 10)]
    int sleuthyDifficultyBase = 8;
    
    [SerializeField, Range(1, 10)]
    int sleuthDifficultyMin = 5;

    [SerializeField]
    DifficultyAdjustment sleuthAdjustment = new DifficultyAdjustment() { Success = 2, Fail = 1 };

    public struct CensuredAnomaly
    {
        public string name;
        public bool horror;
    }

    public IEnumerable<CensuredAnomaly> GetCensuredAnomalies()
    {
        foreach (var anomaly in anomalies)
        {
            if (encounteredAnomalies.Contains(anomaly.id))
            {
                yield return new CensuredAnomaly() { name = anomaly.humanizedName, horror = anomaly.horror };
            } else
            {
                var name = anomaly.humanizedName;
                yield return new CensuredAnomaly() { name = Regex.Replace(name, @"[A-Za-z0-9]", "?"), horror = anomaly.horror };
            }

        }
    }

    IEnumerable<AnomalySetting> anomalyHistory =>
        encounteredAnomalies.Select(id =>
        {
            if (string.IsNullOrEmpty(id)) return null;

            return anomalies.FirstOrDefault(a => a.id == id);
        });

    IEnumerable<AnomalySetting> CandidateAnomalies(bool scary)
    {

        var history = anomalyHistory.ToList();

        // Count which rooms have gotten anomalis so far
        var roomCount = new Dictionary<OfficeRoom, int>();
        foreach (var hist in history)
        {
            if (hist == null) continue;

            foreach (var room in hist.room.AllFlags())
            {
                if (!roomCount.ContainsKey(room))
                {
                    roomCount[room] = 0;
                } else
                {
                    roomCount[room]++;
                }
            }
        }

        System.Func<OfficeRoom, float> roomOrder = (OfficeRoom room) =>
        {
            float sum = 0;
            foreach (var pureRoom in room.AllFlags())
            {
                if (roomCount.TryGetValue(pureRoom, out var value))
                {
                    sum += value;
                }
            }

            return sum;
        };

        var unsued = anomalies.Where(a => !history.Contains(a)).ToList();

        if (unsued.Count > 0)
        {
            Debug.Log("Still have unused anomalies");
            return unsued
                .OrderBy(a => a.horror != scary)
                .ThenBy(a => Mathf.Abs(a.difficulty - WantedDifficulty))
                .ThenBy(a => roomOrder(a.room));
        }

        Debug.Log("Reusing anomalies");
        if (scary)
        {
            return anomalies
                .OrderBy(a => !a.horror)
                .ThenBy(a => history.LastIndexOf(a) / selectFromFirstNCandidates)
                .ThenBy(a => Mathf.Abs(a.difficulty - WantedDifficulty))
                .ThenBy(a => roomOrder(a.room));
        }

        return anomalies
            .OrderBy(a => a.horror)
            .ThenBy(a => history.LastIndexOf(a) / selectFromFirstNCandidates)
            .ThenBy(a => Mathf.Abs(a.difficulty - WantedDifficulty))
            .ThenBy(a => roomOrder(a.room));
    }

    [SerializeField, Header("Debug")]
    string predefAnomaly;

    [ContextMenu("Load predefined anomaly")]
    private void LoadPredef()
    {
        activeAnomaly = anomalies.FirstOrDefault(a => a.id == predefAnomaly);
        Debug.Log($"AnomalyManager: Hotloading '{predefAnomaly}' and it is {Weekday} in week {WeekNumber} with {(activeAnomaly == null ? "a regular office" : activeAnomaly.ToString())}");
        OnSetAnomaly?.Invoke(activeAnomaly?.id);
    }

    public AnomalySetting activeAnomaly { get; private set; }


    #region Save State
    [System.Serializable]
    public enum PreviousDayOutcome { None, Positive, Negative };

    [System.Serializable]
    public class AnomalyManagerSaveData
    {
        public List<string> encounteredAnomalies;
        public List<string> missedAnomalies;
        public List<AnomalyType> weekPlan;
        public int weekNumber;
        public int wantedDifficulty;
        public string activeAnomaly;
        public Weekday weekday;
        public bool won;
        public PreviousDayOutcome previousDayOutcome;
        public ExitType previousDayExit;

        public AnomalyManagerSaveData(AnomalyManager manager)
        {
            encounteredAnomalies = new List<string>(manager.encounteredAnomalies);
            missedAnomalies = new List<string>(manager.missedAnomalies);
            weekNumber = manager.WeekNumber;
            weekday = manager.Weekday;
            wantedDifficulty = manager.difficultyOffset;
            activeAnomaly = manager.activeAnomaly?.id;
            won = manager.won;
            previousDayOutcome = manager.prevDayOutcome;
            previousDayExit = manager.prevDayExit;
            weekPlan = manager.weekPlanSet ? manager.weekPlan.Serialized() : null;
        }

        public AnomalyManagerSaveData()
        {
            encounteredAnomalies = new List<string>();
            missedAnomalies = new List<string>();
            weekNumber = 0;
            wantedDifficulty = LEGACY_START_DIFFICULTY;
            activeAnomaly = null;
            weekday = Weekday.Monday;
            won = false;
            previousDayOutcome = PreviousDayOutcome.None;
            previousDayExit = ExitType.None;
            weekPlan = null;
        }

        public override string ToString() =>
            $"<AnomSave {weekday} {weekNumber} Anom:{activeAnomaly}>";
    }

    List<string> encounteredAnomalies = new List<string>();
    List<string> missedAnomalies = new List<string>();

    const int LEGACY_START_DIFFICULTY = 3;

    int difficultyOffset;
    int WantedDifficulty
    {
        get
        {
            switch (WWSettings.AnomalyDifficulty.Value)
            {
                case AnomalyDifficulty.Clear:
                    return Mathf.Clamp(
                        clearDifficultyBase + difficultyOffset, 
                        1, 
                        clearDifficultyMax);
                case AnomalyDifficulty.Balanced:
                    return Mathf.Clamp(
                        balancedDifficultyBase + difficultyOffset, 
                        1, 
                        10);
                case AnomalyDifficulty.Sleuthy:
                    return Mathf.Clamp(
                        sleuthyDifficultyBase + difficultyOffset, 
                        sleuthDifficultyMin, 
                        10);
            }

            throw new System.ArgumentException($"{WWSettings.AnomalyDifficulty.Value} not supported");
        }
    }

    int _weekNumber;
    public int WeekNumber => _weekNumber;

    Weekday _weekday;
    public Weekday Weekday => _weekday;

    bool weekPlanSet;
    AnomalyPlan weekPlan;

    bool won;
    public bool WonGame => won;

    PreviousDayOutcome prevDayOutcome = PreviousDayOutcome.None;
    public PreviousDayOutcome PrevDayOutcome => prevDayOutcome;

    ExitType prevDayExit = ExitType.None;
    public ExitType PrevDayExit => prevDayExit;

    public void ResetProgress()
    {
        _weekday = Weekday.Monday;
        _weekNumber = 0;
        difficultyOffset = 0;
        missedAnomalies.Clear();
        encounteredAnomalies.Clear();
        activeAnomaly = null;
        anomalyLoaded = false;
        won = false;
        SetWeekPlan();
        Debug.Log($"AnomaliesManager: Set first week plan to {weekPlan}");
    }

    #endregion


    #region Save / Load
    public AnomalyManagerSaveData Save() => 
        new AnomalyManagerSaveData(this);

    public int OnLoadPriority => 100000;

    public void OnLoadWWSave(WWSave save)
    {
        var anomalies = save.anomalies ?? new AnomalyManagerSaveData();

        var stringVersion = save.environment.version;
        if (System.Version.TryParse(stringVersion, out var version))
        {
            if (version < new System.Version(0, 2, 0))
            {
                anomalies.wantedDifficulty -= LEGACY_START_DIFFICULTY;
            }
        }
        encounteredAnomalies.Clear();
        encounteredAnomalies.AddRange(anomalies.encounteredAnomalies.Where(a => a != null));

        missedAnomalies.Clear();
        missedAnomalies.AddRange(anomalies.missedAnomalies);

        _weekNumber = anomalies.weekNumber;
        _weekday = anomalies.weekday;

        if (anomalies.weekPlan != null)
        {
            weekPlan = new AnomalyPlan(anomalies.weekPlan);
            Debug.Log("AnomalyManager: Loaded weekplan");
        } else
        {
            SetWeekPlan();
            Debug.LogWarning("AnomalyManager: There was no weekplan in the save");
        }
        weekPlanSet = true;

        prevDayOutcome = save.anomalies.previousDayOutcome;
        prevDayExit = save.anomalies.previousDayExit;

        difficultyOffset = anomalies.wantedDifficulty;

        if (BBFight.FightStatus == FightStatus.Died)
        {
            // We died, this doesn't mean we need harder or easier difficulty on anomalies
            prevDayOutcome = PreviousDayOutcome.Negative;
            SetWeekPlan();
            SetAnomalyOfTheDay(false);
        } else
        {
            activeAnomaly =
                this.anomalies.FirstOrDefault(a => a.id == anomalies.activeAnomaly);
        }

        won = anomalies.won;

        anomalyLoaded = true;

        Debug.Log($"AnomalyManager: Loaded save {save.anomalies} and it's {Weekday} in week {WeekNumber} with {(activeAnomaly == null ? "a regular office" : activeAnomaly.ToString())}");
        OnSetAnomaly?.Invoke(activeAnomaly?.id);
    }

    void SetWeekPlan()
    {
        var options = weekPlans.Where(plan => plan.AppliesToWeek(WeekNumber)).ToList();
        if (options.Count() == 0)
        {
            options = weekPlans;
        }

        Debug.Log($"AnomalyManager: Seting new week plan");
        weekPlan = options.GetRandomElementOrDefault(() => AnomalyPlan.RandomizedPlan());
        weekPlanSet = true;
        Debug.Log($"AnomalyManager: Set new week plan {weekPlan}");
    }

    void SetAnomalyOfTheDay(bool emitEvent = true)
    {
        if (!weekPlanSet)
        {
            SetWeekPlan();
        }

        var type = weekPlan.GetPlan(Weekday);

        Debug.Log($"AnomalyManager: {Weekday} is {type}");

        switch (type)
        {
            case AnomalyType.NormalOffice:
                activeAnomaly = null;
                break;
            case AnomalyType.Anomaly:
                activeAnomaly = CandidateAnomalies(false)
                    .Take(selectFromFirstNCandidates)
                    .Shuffle()
                    .FirstOrDefault();
                break;
            case AnomalyType.ScaryAnomaly:
                activeAnomaly = CandidateAnomalies(true)
                    .Take(selectFromFirstNCandidates)
                    .Shuffle()
                    .FirstOrDefault();
                break;

        }

        Debug.Log($"AnomalyManager: It's a new day and it is {Weekday} in week {WeekNumber} with {(activeAnomaly == null ? "a regular office" : activeAnomaly.ToString())} {weekPlan}");
        if (emitEvent)
        {
            OnSetAnomaly?.Invoke(activeAnomaly?.id);
        }
    }

    public void OnLoad<T>(T save) where T : new()
    {
        if (save is WWSave)
        {
            OnLoadWWSave(save as WWSave);
        }
    }

    #endregion

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        ExitTrigger.OnExitOffice += ExitTrigger_OnExitOffice;
        TiledDungeon.OnDungeonLoad += TiledDungeon_OnDungeonLoad;
    }

    private void OnDisable()
    {
        ExitTrigger.OnExitOffice -= ExitTrigger_OnExitOffice;
        TiledDungeon.OnDungeonLoad -= TiledDungeon_OnDungeonLoad;
    }

    private void TiledDungeon_OnDungeonLoad(TiledDungeon dungeon, bool fromSave)
    {
        if (!anomalyLoaded)
        {
            SetAnomalyOfTheDay();
        }
    }


    public void DeathByAnomaly() =>
        ExitTrigger_OnExitOffice(ExitType.AnomalyDeath);

    public void FailBossBattle() => 
        ExitTrigger_OnExitOffice(ExitType.BossDeath);

    DifficultyAdjustment ActiveAdjustmentModel()
    {
        switch (WWSettings.AnomalyDifficulty.Value)
        {
            case AnomalyDifficulty.Clear:
                return clearAdjustments;
            case AnomalyDifficulty.Balanced:
                return balancedAdjustment;
            case AnomalyDifficulty.Sleuthy:
                return sleuthAdjustment;
        }

        throw new System.ArgumentException($"{WWSettings.AnomalyDifficulty.Value} not supported");
    }

    private void ExitTrigger_OnExitOffice(ExitType exitType)
    {
        anomalyLoaded = false;
        bool success = false;

        prevDayExit = exitType;

        if (exitType.Either(ExitType.FireEscape, ExitType.AnomalyDeath) && activeAnomaly != null)
        {
            encounteredAnomalies.Add(activeAnomaly.id);
            success = exitType == ExitType.FireEscape;
        } else if (exitType.Either(ExitType.MainExit, ExitType.BossDeath) && activeAnomaly == null)
        {
            success = exitType == ExitType.MainExit;
        } else if (activeAnomaly != null)
        {
            missedAnomalies.Add(activeAnomaly.id);
        }

        Debug.Log($"AnomalyManager: Exit {success} of type {exitType}, fight: {BBFight.FightStatus} {BBFight.BaseDifficulty}");

        if (success)
        {
            prevDayOutcome = PreviousDayOutcome.Positive;
            _weekday = Weekday.NextDay();

            difficultyOffset += ActiveAdjustmentModel().Success;

            activeAnomaly = null;

            if (Weekday == Weekday.Monday)
            {
                _weekNumber++;
                SetWeekPlan();
                won = true;
                Debug.Log($"AnomalyManager: We won the game in week {WeekNumber}");
                WWSaveSystem.instance.AutoSave();

                if (crossfader != null)
                {
                    crossfader.FadeIn(LoadVictoryScene, keepUIAfterFaded: true);
                } else
                {
                    LoadVictoryScene();
                }
            } else
            {
                Debug.Log($"AnomalyManager: Correct exit ({activeAnomaly}), going to {Weekday} {WeekNumber}");
                WWSaveSystem.instance.AutoSave();

                if (crossfader != null)
                {
                    crossfader.FadeIn(LoadOfficeScene, keepUIAfterFaded: true);
                } else
                {
                    LoadOfficeScene();
                }
            }
        } else
        {
            prevDayOutcome = PreviousDayOutcome.Negative;
            if (!WWSettings.EasyMode.Value)
            {
                _weekday = Weekday.Monday;
            } else
            {

                _weekday = _weekday.PreviousDay(wrapWeek: false);
            }

            _weekNumber++;

            if (activeAnomaly != null)
            {
                // Only lower anomaly difficulty when player misses one
                difficultyOffset -= ActiveAdjustmentModel().Fail;
                missedAnomalies.Add(activeAnomaly.id);
            }

            Debug.Log($"AnomalyManager: Wrong exit ({activeAnomaly}), going to {Weekday} {WeekNumber}");

            SetWeekPlan();
            activeAnomaly = null;

            WWSaveSystem.instance.AutoSave();

            if (crossfader != null)
            {
                crossfader.FadeIn(LoadOfficeScene, keepUIAfterFaded: true);
            } else
            {
                LoadOfficeScene();
            }
        }

        OnSetDay?.Invoke(Weekday);
    }

    void LoadOfficeScene()
    {
        SceneManager.LoadScene("OfficeScene");
    }

    void LoadVictoryScene()
    {
        SceneManager.LoadScene("VictoryScene");
    }

    [ContextMenu("Info")]
    void Info()
    {
        Debug.Log($"AnomalyManager: {Weekday} week {WeekNumber} prev day was {prevDayExit} {prevDayOutcome} {activeAnomaly}\nEncountered: {string.Join(", ", encounteredAnomalies)}\nMissed: {string.Join(", ", missedAnomalies)}");
    }

    [ContextMenu("Summarize anomalies")]
    void SummarizeAnomalies()
    {
        Dictionary<OfficeRoom, int> rooms = new Dictionary<OfficeRoom, int>();
        Dictionary<int, int> difficulties = new Dictionary<int, int>();
        int horrors = 0;
        int noHorrors = 0;

        foreach (var anom in anomalies)
        {
            if (anom.horror)
            {
                horrors++;
            } else
            {
                noHorrors++;
            }

            foreach (var room in anom.room.AllFlags())
            {
                if (rooms.ContainsKey(room))
                {
                    rooms[room]++;
                } else
                {
                    rooms[room] = 1;
                }
            }

            if (difficulties.ContainsKey(anom.difficulty))
            {
                difficulties[anom.difficulty]++;
            } else
            {
                difficulties[anom.difficulty] = 1;
            }
        }

        Debug.Log($"{noHorrors} non-horror and {horrors} scary anomalies");
        Debug.Log(string.Join(", ", rooms.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
        Debug.Log($"Difficulties: {string.Join(", ", difficulties.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
    }

    [ContextMenu("Set Sunday")]
    void SetSunday()
    {
        _weekday = Weekday.Sunday;
        OnSetDay?.Invoke(_weekday);
    }
}
