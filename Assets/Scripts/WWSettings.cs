using LMCore.IO;

public enum AnomalyDifficulty
{
    Clear,
    Balanced,
    Sleuthy
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
}
