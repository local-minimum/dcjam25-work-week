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
        }
    }

    void DisableManager()
    {
        GetComponent<TDEnemy>().Paused = true;

        GetComponent<GridEntity>().enabled = false;
        GetComponent<TDDangerZone>().enabled = false;
        GetComponentInChildren<TDEnemyPerception>(true).enabled = false;

        transform.HideAllChildren();
    }

    public void EnableManager()
    {
        GetComponent<TDEnemy>().Paused = false;

        GetComponent<GridEntity>().enabled = true;
        GetComponent<TDDangerZone>().enabled = true;
        GetComponentInChildren<TDEnemyPerception>(true).enabled = true;

        transform.ShowAllChildren();
    }
}
