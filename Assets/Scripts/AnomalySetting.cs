using System;
using UnityEngine;

[Serializable, Flags]
public enum OfficeRoom
{
    Entrance = 1,
    Main = 2,
    MeetingRoom1 = 4,
    MeetingRoom2 = 8,
    StorageRoom = 16,
    BossRoom = 32,
    LunchRoom = 64,
    Restrooms = 128,
    ExitCorridor = 256,
}

[Serializable]
public class AnomalySetting
{
    public string id;
    public string humanizedName;
    public OfficeRoom room;
    [Range(1, 10)]
    public int difficulty = 1;
    public bool horror;

    public override string ToString() =>
        $"<Anomaly {id} in {room}, difficulty {difficulty}{(horror ? " horror!" : "")}>";
}
