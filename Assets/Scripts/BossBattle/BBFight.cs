using LMCore.UI;
using TMPro;
using UnityEngine;

public delegate void DifficultyEvent(int level);
public enum FightStatus { None, InProgress, Survived, Died };

public class BBFight : MonoBehaviour
{
    [SerializeField]
    AudioSource speaker;

    [SerializeField]
    AudioClip winMusic;

    [SerializeField]
    AudioClip failMusic;

    public static FightStatus FightStatus { get; set; } = FightStatus.None;

    public static int BaseDifficulty { get; set; } = 1;

    public static event DifficultyEvent OnChangeDifficulty;

    [SerializeField]
    BBPlayerController player;

    [SerializeField]
    TextMeshProUGUI RemainingUI;

    [SerializeField]
    float survivalTime = 30f;

    [SerializeField]
    float increaseDifficultyEvery = 10f;

    float startTime;
    float nextDifficulty;
    bool started;

    int difficulty = 1;

    float timeScalePerDifficulty = 0.25f;

    float Remaining =>
        started ? survivalTime - (Time.timeSinceLevelLoad - startTime) : survivalTime;

    void SyncCountdown()
    {
        RemainingUI.text = $"Survive for {Remaining.ToString("##")}s";
    }

    private void Start()
    {
        difficulty = BaseDifficulty;
        FightStatus = FightStatus.InProgress;
    }

    private void OnEnable()
    {
        player.OnHealthChange += Player_OnHealthChange;
        BBFaceController.OnStartSpitting += BBFaceController_OnStartSpitting;

        SyncCountdown();
    }


    private void OnDisable()
    {
        player.OnHealthChange -= Player_OnHealthChange;
        BBFaceController.OnStartSpitting -= BBFaceController_OnStartSpitting;
    }

    private void BBFaceController_OnStartSpitting()
    {
        started = true;
        startTime = Time.timeSinceLevelLoad;
        nextDifficulty = startTime + increaseDifficultyEvery;
        Time.timeScale = 1f + (difficulty - 1) * timeScalePerDifficulty;
    }

    private void Player_OnHealthChange(int health)
    {
        if (health <= 0)
        {
            FightStatus = FightStatus.Died;

            speaker.clip = failMusic;
            speaker.loop = false;
            speaker.Play();

            DelayedLoadingSave("Cornered!");
        }
    }

    [SerializeField]
    Crossfader crossfader;

    void DelayedLoadingSave(string msg)
    {
        Time.timeScale = 0f;
        enabled = false;
        crossfader.FadeIn(LoadSave, msg, keepUIAfterFaded: true);
    }

    void LoadSave()
    {
        Time.timeScale = 1f;
        WWSaveSystem.SafeInstance.LoadAutoSave();
    }

    private void Update()
    {
        if (started) SyncCountdown();
        if (Remaining <= 0f)
        {
            FightStatus = FightStatus.Survived;

            speaker.clip = winMusic;
            speaker.loop = false;
            speaker.Play();

            DelayedLoadingSave("Somehow you dodged that!");
            return;
        }

        if (Time.timeSinceLevelLoad > nextDifficulty)
        {

            difficulty++;
            OnChangeDifficulty?.Invoke(difficulty);
            Time.timeScale = 1f + (difficulty - 1) * timeScalePerDifficulty;

            nextDifficulty = Time.timeSinceLevelLoad + increaseDifficultyEvery;
        }
    }
}
