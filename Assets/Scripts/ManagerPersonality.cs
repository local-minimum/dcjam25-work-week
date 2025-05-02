using LMCore.IO;

public enum AnomalyDifficulty
{
    Clear,
    Balanced,
    Sleuthy
}

public enum ManagerPersonality
{
    Golfer,
    Steward,
    Zealous
}

public static class WWSettings 
{
    public static GameSettings.BoolSetting MonologueHints => 
        GameSettings.GetCustomBool("gameplay.monologues", true);

    public static GameSettings.BoolSetting EasyMode => 
        GameSettings.GetCustomBool("gameplay.easymode", false);


    static GameSettings.EnumSetting<AnomalyDifficulty> _anomalyDifficulty;

    public static GameSettings.EnumSetting<AnomalyDifficulty> AnomalyDifficulty
    {
        get
        {
            if (_anomalyDifficulty != null) return _anomalyDifficulty;

            _anomalyDifficulty = new GameSettings.EnumSetting<AnomalyDifficulty>(
                GameSettings.GetCustomEnumKey("gameplay.anomalies"),
                global::AnomalyDifficulty.Balanced);

            return _anomalyDifficulty;
        }
    }

    static GameSettings.EnumSetting<ManagerPersonality> _managerPersonality;

    public static GameSettings.EnumSetting<ManagerPersonality> ManagerPersonality
    {
        get
        {
            if (_managerPersonality != null) return _managerPersonality;

            _managerPersonality = new GameSettings.EnumSetting<ManagerPersonality>(
                GameSettings.GetCustomEnumKey("gameplay.manager"),
                global::ManagerPersonality.Steward);

            return _managerPersonality;
        }
    }
}
