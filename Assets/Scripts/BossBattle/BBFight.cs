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

    [SerializeField]
    MenusManager pauseMenuManager;

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

    [SerializeField]
    float difficultyIncreaseDecay = 0.5f;

    [SerializeField]
    float minDifficultyIncreaseFreq = 2f; 

    float startTime;
    float nextDifficulty;
    bool started;

    int difficulty = 1;

    float timeScalePerDifficulty = 0.25f;

    float Remaining =>
        started ? Mathf.Max(0, survivalTime - (Time.realtimeSinceStartup - startTime)) : survivalTime;

    void SyncCountdown()
    {
        var remaining = Remaining;
        if (remaining <= 0)
        {
            RemainingUI.text = $"Survive for 0s";
        }
        RemainingUI.text = $"Survive for {remaining.ToString("##")}s";
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

        pauseMenuManager.OnResumeGame += PauseMenuManager_OnResumeGame;

        SyncCountdown();
    }

    private void OnDisable()
    {
        player.OnHealthChange -= Player_OnHealthChange;
        BBFaceController.OnStartSpitting -= BBFaceController_OnStartSpitting;
        pauseMenuManager.OnResumeGame -= PauseMenuManager_OnResumeGame;
    }

    private void PauseMenuManager_OnResumeGame(float time)
    {
        nextDifficulty += time;
        startTime += time;
    }

    private void BBFaceController_OnStartSpitting()
    {
        started = true;
        startTime = Time.realtimeSinceStartup;
        ElevateDifficulty(false);
    }

    void ElevateDifficulty(bool elevate)
    {
        nextDifficulty = Time.realtimeSinceStartup + increaseDifficultyEvery;
        if (elevate)
        {
            difficulty++;
            increaseDifficultyEvery = Mathf.Max(minDifficultyIncreaseFreq, increaseDifficultyEvery - difficultyIncreaseDecay);
        }
        Time.timeScale = 1f + (difficulty - 1) * timeScalePerDifficulty;
        Debug.Log($"BBFight: Difficulty now at {difficulty}, next increase is {nextDifficulty} done every {increaseDifficultyEvery}");
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
        if (Time.timeScale == 0) return;

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

        if (Time.realtimeSinceStartup > nextDifficulty)
        {
            ElevateDifficulty(true);
            OnChangeDifficulty?.Invoke(difficulty);
        }
    }
}
