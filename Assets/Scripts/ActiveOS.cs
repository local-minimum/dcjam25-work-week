using LMCore.UI;
using System.Linq;
using TMPro;
using UnityEngine;

public delegate void ReleasePlayerEvent();

public class ActiveOS : MonoBehaviour
{
    public static event ReleasePlayerEvent OnReleasePlayer;
    
    [SerializeField]
    TextMeshProUGUI DayField;

    [SerializeField]
    Transform[] overlays;

    [SerializeField]
    VirtualPointer pointer;

    private void OnEnable()
    {
        AnomalyManager.OnSetDay += AnomalyManager_OnSetDay;
        StartPositionCustom.OnCapturePlayer += StartPositionCustom_OnCapturePlayer;
        StartPositionCustom.OnReleasePlayer += StartPositionCustom_OnReleasePlayer;
    }

    private void OnDisable()
    {
        AnomalyManager.OnSetDay += AnomalyManager_OnSetDay;
        StartPositionCustom.OnCapturePlayer -= StartPositionCustom_OnCapturePlayer;
        StartPositionCustom.OnReleasePlayer -= StartPositionCustom_OnReleasePlayer;
    }

    bool showPointer;

    private void StartPositionCustom_OnReleasePlayer(LMCore.Crawler.GridEntity player)
    {
        showPointer = false;
        pointer.enabled = false;
    }

    private void StartPositionCustom_OnCapturePlayer(LMCore.Crawler.GridEntity player)
    {
        showPointer = true;
        pointer.enabled = true;
    }

    private void Start()
    {
        AnomalyManager_OnSetDay(AnomalyManager.instance.Weekday);
        pointer.enabled = showPointer;
    }

    private void AnomalyManager_OnSetDay(Weekday day)
    {
        DayField.text = day.ToString();
    }

    public void ReleasePlayer()
    {
        OnReleasePlayer?.Invoke();
    }

    public void FocusApp(ActiveOSApp app)
    {
        if (overlays == null || overlays.Length == 0)
        {
            app.transform.SetAsLastSibling();
        }
        var firstOverlay = overlays.Min(o => o.GetSiblingIndex());
        app.transform.SetSiblingIndex(Mathf.Min(firstOverlay - 1));
    }
}
