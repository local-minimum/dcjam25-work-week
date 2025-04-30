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

        public List<AnomalyType> Serialized() =>
            new List<AnomalyType>() { Monday, Tueday, Wednesday, Thursday, Friday, Saturday, Sunday };

        public override string ToString() =>
            $"<Plan Mo:{Monday} Tu:{Tueday} We:{Wednesday} Th:{Thursday} Fr:{Friday} Sa:{Saturday} Su:{Sunday}>";
    }

    bool anomalyLoaded;

    [SerializeField]
    Crossfader crossfader;

    [SerializeField]
    List<AnomalyPlan> weekPlans = new List<AnomalyPlan>();

    [SerializeField]
    List<AnomalySetting> anomalies = new List<AnomalySetting>();


    public IEnumerable<string> GetCensuredAnomalies()
    {
        foreach (var anomaly in anomalies)
        {
            if (encounteredAnomalies.Contains(anomaly.id))
            {
                yield return anomaly.humanizedName;
            } else
            {
                var name = anomaly.humanizedName;
                yield return Regex.Replace(name, @"[A-Za-z0-9]", "?");
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
        var roomCount = new Dictionary<OfficeRoom, int>();

        var history = anomalyHistory.ToList();

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
            return unsued
                .OrderBy(a => Mathf.Abs(a.difficulty - wantedDifficulty))
                .ThenBy(a => roomOrder(a.room));
        }

        if (scary)
        {
            return anomalies
                .OrderBy(a => history.LastIndexOf(a) / selectFromFirstNCandidates)
                .ThenBy(a => !a.horror)
                .ThenBy(a => Mathf.Abs(a.difficulty - wantedDifficulty))
                .ThenBy(a => roomOrder(a.room));
        }

        return anomalies
            .OrderBy(a => history.LastIndexOf(a) / selectFromFirstNCandidates)
            .ThenBy(a => a.horror)
            .ThenBy(a => Mathf.Abs(a.difficulty - wantedDifficulty))
            .ThenBy(a => roomOrder(a.room));
    }


    [SerializeField]
    int selectFromFirstNCandidates = 3;

    [SerializeField]
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
            wantedDifficulty = manager.wantedDifficulty;
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
            wantedDifficulty = START_DIFFICULTY;
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

    const int START_DIFFICULTY = 3;
    int wantedDifficulty = START_DIFFICULTY;

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
        wantedDifficulty = START_DIFFICULTY;
        missedAnomalies.Clear();
        encounteredAnomalies.Clear();
        activeAnomaly = null;
        anomalyLoaded = false;
        won = false;
        weekPlan = new AnomalyPlan();
    }

    #endregion


    #region Save / Load
    public AnomalyManagerSaveData Save() => 
        new AnomalyManagerSaveData(this);

    public int OnLoadPriority => 100000;

    public void OnLoadWWSave(WWSave save)
    {
        var anomalies = save.anomalies ?? new AnomalyManagerSaveData();

        encounteredAnomalies.Clear();
        encounteredAnomalies.AddRange(anomalies.encounteredAnomalies);

        missedAnomalies.Clear();
        missedAnomalies.AddRange(anomalies.missedAnomalies);

        _weekNumber = anomalies.weekNumber;
        _weekday = anomalies.weekday;

        if (anomalies.weekPlan != null)
        {
            weekPlan = new AnomalyPlan(anomalies.weekPlan);
        } else
        {
            SetWeekPlan();
            Debug.LogWarning("AnomalyManager: There was no weekplan in the save");
        }
        weekPlanSet = true;

        prevDayOutcome = save.anomalies.previousDayOutcome;
        prevDayExit = save.anomalies.previousDayExit;

        if (BBFight.FightStatus == FightStatus.Died)
        {
            wantedDifficulty = Mathf.Max(1, wantedDifficulty - 1);
            prevDayOutcome = PreviousDayOutcome.Negative;
            SetWeekPlan();
            SetAnomalyOfTheDay(false);
        } else
        {
            wantedDifficulty = anomalies.wantedDifficulty;

            activeAnomaly =
                this.anomalies.FirstOrDefault(a => a.id == anomalies.activeAnomaly);
        }

        won = anomalies.won;

        anomalyLoaded = true;

        if (BBFight.FightStatus == FightStatus.Survived)
        {
            wantedDifficulty = Mathf.Min(10, wantedDifficulty + 1);
        }

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

        weekPlan = options.GetRandomElementOrDefault();
        weekPlanSet = true;
        Debug.Log($"AnomalyManager: {weekPlan}");
    }

    void SetAnomalyOfTheDay(bool emitEvent = true)
    {
        if (!weekPlanSet)
        {
            SetWeekPlan();
        }

        var type = weekPlan.GetPlan(Weekday);

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
            encounteredAnomalies.Add(null);
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

            encounteredAnomalies.Add(activeAnomaly?.id);
            wantedDifficulty = Mathf.Min(10, wantedDifficulty + 1);

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
            if (!SettingsMenu.EasyMode.Value)
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
                wantedDifficulty = Mathf.Max(1, wantedDifficulty - 1);
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
