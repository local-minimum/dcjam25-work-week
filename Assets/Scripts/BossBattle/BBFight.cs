using TMPro;
using UnityEngine;

public delegate void DifficultyEvent(int level);

public class BBFight : MonoBehaviour
{
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
            Debug.Log("We lost");
        }
    }

    private void Update()
    {
        if (started) SyncCountdown();
        if (Remaining <= 0f)
        {
            Debug.Log("We win");
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
