using LMCore.IO;
using UnityEngine;

public class MyComputer : MonoBehaviour, IOnLoadSave
{
    [SerializeField]
    Renderer screen;

    [SerializeField]
    Material screenSaverMat;

    private void OnEnable()
    {
        StartPositionCustom.OnReleasePlayer += StartPositionCustom_OnReleasePlayer;
    }

    private void OnDisable()
    {
        StartPositionCustom.OnReleasePlayer -= StartPositionCustom_OnReleasePlayer;
    }

    private void StartPositionCustom_OnReleasePlayer(LMCore.Crawler.GridEntity player)
    {
        screen.material = screenSaverMat;
    }

    public int OnLoadPriority => 1;

    public void OnLoad<T>(T save) where T : new()
    {
        screen.material = screenSaverMat;
    }
}
