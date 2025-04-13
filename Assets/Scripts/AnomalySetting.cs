using System;
using UnityEngine;

[Serializable, Flags]
public enum OfficeRoom
{
    Entrance,
    Main,
    MeetingRoom1,
    MeetingRoom2,
    StorageRoom,
    BossRoom,
    LunchRoom,
    Restrooms,
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
