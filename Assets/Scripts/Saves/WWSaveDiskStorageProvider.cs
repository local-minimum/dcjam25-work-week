using LMCore.IO;
using UnityEngine;

public class WWSaveDiskStorageProvider : DiskStorageProvider<WWSave> 
{
    [ContextMenu("Info")]
    void Info()
    {
        LogStatus(WWSaveSystem.instance.maxSaves);
    }
}
