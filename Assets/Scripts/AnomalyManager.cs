using LMCore.AbstractClasses;
using LMCore.Extensions;
using LMCore.IO;
using LMCore.TiledDungeon;
using LMCore.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public delegate void SetAnomalyEvent(string id);
public delegate void SetDayEvent(Weekday day);

public class AnomalyManager : Singleton<AnomalyManager, AnomalyManager>, IOnLoadSave
{
    public static event SetAnomalyEvent OnSetAnomaly;
    public static event SetDayEvent OnSetDay;

    bool anomalyLoaded;

    [SerializeField]
    Crossfader crossfader;

    [SerializeField]
    List<AnomalySetting> anomalies = new List<AnomalySetting>();

    IEnumerable<AnomalySetting> anomalyHistory =>
        encounteredAnomalies.Select(id =>
        {
            if (string.IsNullOrEmpty(id)) return null;

            return anomalies.FirstOrDefault(a => a.id == id);
        });

    IEnumerable<AnomalySetting> CandidateAnomalies
    {
        get
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

            return anomalies
                .OrderBy(a => history.LastIndexOf(a) / selectFromFirstNCandidates)
                .ThenBy(a => Mathf.Abs(a.difficulty - wantedDifficulty))
                .ThenBy(a => roomOrder(a.room));
        }
    }


    [SerializeField]
    int maxDaysWithSameState = 3;

    [SerializeField]
    float anomalyBaseBias = 0.6f;

    [SerializeField]
    int selectFromFirstNCandidates = 5;

    [SerializeField]
    string predefAnomaly;

    [ContextMenu("Load predefined anomaly")]
    private void LoadPredef()
    {
        activeAnomaly = anomalies.FirstOrDefault(a => a.id == predefAnomaly);
        Debug.Log($"AnomalyManger: Hotloading '{predefAnomaly}' and it is {Weekday} in week {WeekNumber} with {(activeAnomaly == null ? "a regular office" : activeAnomaly.ToString())}");
        OnSetAnomaly?.Invoke(activeAnomaly?.id);
    }

    public AnomalySetting activeAnomaly { get; private set; }


    #region Save State
    [System.Serializable]
    public class AnomalyManagerSaveData
    {
        public List<string> encounteredAnomalies;
        public List<string> missedAnomalies;
        public int weekNumber;
        public int wantedDifficulty;
        public string activeAnomaly;
        public Weekday weekday;

        public AnomalyManagerSaveData(AnomalyManager manager)
        {
            encounteredAnomalies = new List<string>(manager.encounteredAnomalies);
            missedAnomalies = new List<string>(manager.missedAnomalies);
            weekNumber = manager.WeekNumber;
            weekday = manager.Weekday;
            wantedDifficulty = manager.wantedDifficulty;
            activeAnomaly = manager.activeAnomaly?.id;
        }

        public AnomalyManagerSaveData()
        {
            encounteredAnomalies = new List<string>();
            missedAnomalies = new List<string>();
            weekNumber = 0;
            wantedDifficulty = START_DIFFICULTY;
            activeAnomaly = null;
            weekday = Weekday.Monday;
        }
    }

    List<string> encounteredAnomalies = new List<string>();
    List<string> missedAnomalies = new List<string>();

    const int START_DIFFICULTY = 3;
    int wantedDifficulty = START_DIFFICULTY;

    int _weekNumber;
    public int WeekNumber => _weekNumber;

    Weekday _weekday;
    public Weekday Weekday => _weekday;

    public void ResetProgress()
    {
        _weekday = Weekday.Monday;
        _weekNumber = 0;
        wantedDifficulty = START_DIFFICULTY;
        missedAnomalies.Clear();
        encounteredAnomalies.Clear();
        activeAnomaly = null;
        anomalyLoaded = false;
    }

    #endregion


    #region Save / Load
    public AnomalyManagerSaveData Save() => 
        new AnomalyManagerSaveData(this);

    public int OnLoadPriority => 100;

    public void OnLoadWWSave(WWSave save)
    {
        var anomalies = save.anomalies ?? new AnomalyManagerSaveData();
        encounteredAnomalies.Clear();
        encounteredAnomalies.AddRange(anomalies.encounteredAnomalies);

        missedAnomalies.Clear();
        missedAnomalies.AddRange(anomalies.missedAnomalies);

        _weekNumber = anomalies.weekNumber;
        _weekday = anomalies.weekday;

        wantedDifficulty = anomalies.wantedDifficulty;

        activeAnomaly =
            this.anomalies.FirstOrDefault(a => a.id == anomalies.activeAnomaly);

        anomalyLoaded = true;

        Debug.Log($"AnomalyManger: Loaded save and it's {Weekday} in week {WeekNumber} with {(activeAnomaly == null ? "a regular office" : activeAnomaly.ToString())}");
        OnSetAnomaly?.Invoke(activeAnomaly?.id);
    }

    [SerializeField()]
    void SetAnomalyOfTheDay()
    {
        if (WeekNumber == 0 && Weekday == Weekday.Monday)
        {
            activeAnomaly = null;
        } else
        {
            var lastDays =
                anomalyHistory.TakeLast(maxDaysWithSameState).ToList();

            if (lastDays.All(a => a == null) && lastDays.Count == maxDaysWithSameState)
            {
                activeAnomaly = CandidateAnomalies.Take(selectFromFirstNCandidates).Shuffle().FirstOrDefault();
            } else if (lastDays.All(a => a != null) && lastDays.Count == maxDaysWithSameState)
            {
                activeAnomaly = null;
            } else
            {
                activeAnomaly = Random.value < anomalyBaseBias ? CandidateAnomalies.Shuffle().FirstOrDefault() : null;
            }
        }

        Debug.Log($"AnomalyManger: It's a new day and it is {Weekday} in week {WeekNumber} with {(activeAnomaly == null ? "a regular office" : activeAnomaly.ToString())}");
        OnSetAnomaly?.Invoke(activeAnomaly?.id);
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

        // TODO: Some fancy transition perhaps
        if (success)
        {
            _weekday = Weekday.NextDay();

            encounteredAnomalies.Add(activeAnomaly?.id);
            wantedDifficulty = Mathf.Min(10, wantedDifficulty + 1);

            activeAnomaly = null;

            if (Weekday == Weekday.Monday)
            {
                _weekNumber++;
                // We won!
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
            _weekday = Weekday.Monday;

            _weekNumber++;

            if (activeAnomaly != null)
            {
                // Only lower anomaly difficulty when player misses one
                wantedDifficulty = Mathf.Max(1, wantedDifficulty - 1);
                missedAnomalies.Add(activeAnomaly.id);
            }

            Debug.Log($"AnomalyManager: Wrong exit ({activeAnomaly}), going to {Weekday} {WeekNumber}");
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
        Debug.Log($"AnomalyManager: {Weekday} week {WeekNumber} {activeAnomaly}\nEncountered: {string.Join(", ", encounteredAnomalies)}\nMissed: {string.Join(", ", missedAnomalies)}");
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
}
