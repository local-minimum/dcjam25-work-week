using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.TiledDungeon;
using LMCore.TiledDungeon.Enemies;
using UnityEngine;

public class ManagerPersonalityController : MonoBehaviour
{
    private void Start()
    {
        if (WWSettings.ManagerPersonality.Value != ManagerPersonality.Zealous)
        {
            DisableManager();
        }

        Debug.Log($"Manager Personality: {WWSettings.ManagerPersonality.Value}");
    }

    private void OnEnable()
    {
        WWSettings.ManagerPersonality.OnChange += ManagerPersonality_OnChange;
    }

    private void OnDisable()
    {
        WWSettings.ManagerPersonality.OnChange -= ManagerPersonality_OnChange;
    }

    private void ManagerPersonality_OnChange(ManagerPersonality value)
    {
        if (value == ManagerPersonality.Golfer)
        {
            DisableManager();
        } else if (value == ManagerPersonality.Zealous) {
            if (!Attentive)
            {
                EnableManager();
            }
        }
    }

    public bool Attentive { get; private set; } = true;

    void DisableManager()
    {
        Attentive = false;

        GetComponent<TDEnemy>().Paused = true;

        GetComponent<GridEntity>().enabled = false;
        GetComponent<TDDangerZone>().enabled = false;
        GetComponentInChildren<TDEnemyPerception>(true).enabled = false;

        transform.HideAllChildren();

        Debug.Log("Manager Personality: not attentive");
    }

    public void EnableManager()
    {
        Attentive = true;

        var enemy = GetComponent<TDEnemy>();
        enemy.Paused = false;
        enemy.ReEnterActiveState();

        GetComponent<GridEntity>().enabled = true;
        GetComponent<TDDangerZone>().enabled = true;
        GetComponentInChildren<TDEnemyPerception>(true).enabled = true;

        transform.ShowAllChildren();

        Debug.Log("Manager Personality: attentive");
    }
}
