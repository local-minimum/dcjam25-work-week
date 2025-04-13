using LMCore.AbstractClasses;
using LMCore.Extensions;
using LMCore.IO;
using LMCore.TiledDungeon;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public delegate void SetAnomalyEvent(string id);

public class AnomalyManager : Singleton<AnomalyManager, AnomalyManager>, IOnLoadSave
{
    public static event SetAnomalyEvent OnSetAnomaly;

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
                if (!roomCount.ContainsKey(hist.room))
                {
                    roomCount[hist.room] = 0;
                } else
                {
                    roomCount[hist.room]++;
                }
            }

            var unsued = anomalies.Where(a => !history.Contains(a)).ToList();

            // TODO: Order by suitable difficulty

            if (unsued.Count > 0)
            {
                return unsued
                    .OrderBy(a => roomCount.TryGetValue(a.room, out int count) ? count : 0);
            }

            return anomalies
                .OrderBy(a => history.LastIndexOf(a) / selectFromFirstNCandidates)
                .ThenBy(a => roomCount.TryGetValue(a.room, out int count) ? count : 0);
        }
    }


    [SerializeField]
    int maxDaysWithSameState = 3;

    [SerializeField]
    float anomalyBaseBias = 0.6f;

    [SerializeField]
    int selectFromFirstNCandidates = 5;

    public AnomalySetting activeAnomaly { get; private set; }


    #region Save State
    public class AnomalyManagerSaveData
    {
        public List<string> encounteredAnomalies;
        public List<string> missedAnomalies;
        public int weekNumber;
        public Weekday weekday;

        public AnomalyManagerSaveData(AnomalyManager manager)
        {
            encounteredAnomalies = new List<string>(manager.encounteredAnomalies);
            missedAnomalies = new List<string>(manager.missedAnomalies);
            weekNumber = manager.WeekNumber;
            weekday = manager.Weekday;
        }
    }

    List<string> encounteredAnomalies = new List<string>();
    List<string> missedAnomalies = new List<string>();

    int _weekNumber;
    public int WeekNumber => _weekNumber;

    Weekday _weekday;
    public Weekday Weekday => _weekday;
    #endregion


    #region Save / Load
    public AnomalyManagerSaveData Save() => 
        new AnomalyManagerSaveData(this);

    public int OnLoadPriority => 100;

    public void OnLoadWWSave(WWSave save)
    {
        encounteredAnomalies.Clear();
        encounteredAnomalies.AddRange(save.encounteredAnomalies);

        _weekNumber = save.weekNumber;
        _weekday = save.weekday;

        SetAnomalyOfTheDay();
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

        Debug.Log($"AnomalyManger: It's {Weekday} in week {WeekNumber} with {(activeAnomaly == null ? "a regular office" : activeAnomaly.ToString())}");
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
        SetAnomalyOfTheDay();
    }


    private void ExitTrigger_OnExitOffice(ExitType exitType)
    {
        bool success = false;

        if (exitType == ExitType.FireEscape && activeAnomaly != null)
        {
            encounteredAnomalies.Add(activeAnomaly.id);
            success = true;
        } else if (exitType == ExitType.MainExit && activeAnomaly == null)
        {
            encounteredAnomalies.Add(null);
            success = true;
        } else if (activeAnomaly != null)
        {
            missedAnomalies.Add(activeAnomaly.id);
        }

        // TODO: Some fancy transition perhaps
        if (success)
        {
            _weekday = Weekday.NextDay();

            encounteredAnomalies.Add(activeAnomaly?.id);

            if (Weekday == Weekday.Monday)
            {
                _weekNumber++;
                // We won!
                Debug.Log($"AnomalyManager: We won the game in week {WeekNumber}");
                SceneManager.LoadScene("VictoryScene");
            } else
            {
                Debug.Log($"AnomalyManager: Correct exit ({activeAnomaly}), going to {Weekday} {WeekNumber}");
                activeAnomaly = null;
                SceneManager.LoadScene("OfficeScene");
            }
        } else
        {
            _weekday = Weekday.Monday;
            _weekNumber++;

            if (activeAnomaly != null)
            {
                missedAnomalies.Add(activeAnomaly.id);
            }

            Debug.Log($"AnomalyManager: Wrong exit ({activeAnomaly}), going to {Weekday} {WeekNumber}");
            activeAnomaly = null;

            SceneManager.LoadScene("OfficeScene");
        }
    }

    [ContextMenu("Info")]
    void Info()
    {
        Debug.Log($"AnomalyManager: {Weekday} week {WeekNumber} {activeAnomaly}\nEncountered: {string.Join(", ", encounteredAnomalies)}\nMissed: {string.Join(", ", missedAnomalies)}");
    }
}
