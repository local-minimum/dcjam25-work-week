using UnityEngine;

[System.Serializable]
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

[System.Serializable]
public class AnomalySetting
{
    public string id;
    public string humanizedName;
    public OfficeRoom room;
    public int difficulty;
    public bool horror;

    public override string ToString() =>
        $"<Anomaly {id} in {room}, difficulty {difficulty}{(horror ? " horror!" : "")}>";
}
