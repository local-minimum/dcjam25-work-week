using LMCore.Extensions;
using System;
using System.Linq;
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

public static class OfficeRoomExtensions
{
    private static string HumanizeRoomFlag(OfficeRoom room)
    {
        switch (room)
        {
            case OfficeRoom.Entrance:
                return "Entrance";
            case OfficeRoom.Main:
                return "Cubicles Area";
            case OfficeRoom.MeetingRoom1:
            case OfficeRoom.MeetingRoom2:
                return "Meeting Room";
            case OfficeRoom.BossRoom:
                return "Managers Office";
            case OfficeRoom.LunchRoom:
                return "Lunchroom";
            case OfficeRoom.Restrooms:
                return "Restroom";
            case OfficeRoom.StorageRoom:
                return "Storage Room";
            case OfficeRoom.ExitCorridor:
                return "Fire Exit Corridor";
            default:
                return null;
        }
    }
    public static string Humanize(this OfficeRoom room) =>
        string.Join(", ", room.AllFlags().Select(r => HumanizeRoomFlag(r)).Where(r => !string.IsNullOrEmpty(r)));
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
