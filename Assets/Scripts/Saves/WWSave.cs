using LMCore.TiledDungeon.SaveLoad;
using System.Collections.Generic;


[System.Serializable]
public class BossBattleSave
{
    public bool triggered;
    public int difficulty;
    public int managerGroggySteps;
}

[System.Serializable]
public enum Weekday { Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday }

public static class WeekdayExtensions
{
    public static Weekday NextDay(this Weekday weekday)
    {
        switch (weekday)
        {
            case Weekday.Monday:
                return Weekday.Tuesday;
            case Weekday.Tuesday:
                return Weekday.Wednesday;
            case Weekday.Wednesday:
                return Weekday.Thursday;
            case Weekday.Thursday:
                return Weekday.Friday;
            case Weekday.Friday:
                return Weekday.Saturday;
            case Weekday.Saturday:
                return Weekday.Sunday;
            case Weekday.Sunday:
                return Weekday.Monday;
            default:
                return Weekday.Monday;
        }
    }
}

[System.Serializable]
public class WWSave : GameSave 
{
    public AnomalyManager.AnomalyManagerSaveData anomalies = new AnomalyManager.AnomalyManagerSaveData();
    public BossBattleSave battle = new BossBattleSave();

    public WWSave(GameSave save)
    {
        saveTime = save.saveTime;

        environment = save.environment;
        disposedItems = save.disposedItems;
        deadEnimies = save.deadEnimies;
        levels = save.levels;
        storyCollections = save.storyCollections;
        player = save.player;
        playerStats = save.playerStats;
    }

    public WWSave() {}
}
