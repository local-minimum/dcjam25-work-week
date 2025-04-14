using UnityEngine;

public class MyComputer : MonoBehaviour
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
}
