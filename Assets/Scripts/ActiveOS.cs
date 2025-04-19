using LMCore.Extensions;
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

    [SerializeField]
    Camera osCam;

    bool osActive;

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
        Debug.Log("ActiveOS: Disabled");
        showPointer = false;
        pointer.enabled = false;
        Cursor.visible = false;
        osActive = false;
        osCam.gameObject.SetActive(false);
        transform.HideAllChildren();
    }

    private void StartPositionCustom_OnCapturePlayer(LMCore.Crawler.GridEntity player)
    {
        Debug.Log("ActiveOS: Enabled");
        showPointer = true;
        pointer.enabled = true;
        osActive = true;
        osCam.gameObject.SetActive(true);
        transform.ShowAllChildren();
    }

    private void Start()
    {
        Debug.Log("ActiveOS: Start");
        AnomalyManager_OnSetDay(AnomalyManager.instance.Weekday);
        pointer.enabled = showPointer;
    }

    private void AnomalyManager_OnSetDay(Weekday day)
    {
        DayField.text = day.ToString();
    }

    public void ReleasePlayer()
    {
        if (!osActive)
        {
            Debug.LogWarning("ActiveOS: OS not active, should not be cicking stuff!");
            return;
        }

        OnReleasePlayer?.Invoke();
    }

    public void FocusApp(ActiveOSApp app)
    {
        if (!osActive)
        {
            Debug.LogWarning("ActiveOS: OS not active, should not be cicking stuff!");
            return;
        }

        if (overlays == null || overlays.Length == 0)
        {
            app.transform.SetAsLastSibling();
        }
        var firstOverlay = overlays.Min(o => o.GetSiblingIndex());
        app.transform.SetSiblingIndex(Mathf.Min(firstOverlay - 1));
    }
}
