using LMCore.AbstractClasses;
using LMCore.Extensions;
using System.Collections.Generic;
using UnityEngine;

public class BBRoomsManager : Singleton<BBRoomsManager, BBRoomsManager>
{
    [SerializeField]
    List<BBRoom> topRooms = new List<BBRoom>();
    [SerializeField]
    List<BBRoom> bottomRooms = new List<BBRoom>();

    public void GetSpawnConnectingRoom(BBRoom triggeringRoom)
    {
        BBRoom prefab = null;
        if (triggeringRoom.Lower)
        {
            prefab = topRooms.GetRandomElementOrDefault();
        } else
        {
            prefab = bottomRooms.GetRandomElementOrDefault();
        }

        if (prefab != null)
        {
            Debug.Log($"RoomsManager is spawning a new room {prefab} {(prefab.Lower ? "L" : "U")} to connect to {triggeringRoom} {(triggeringRoom.Lower ? "L" : "U")}.");
            var room = Instantiate(prefab);
            room.AlignEntryTo(triggeringRoom);
        }
    }
}
