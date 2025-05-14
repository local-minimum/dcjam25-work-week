using LMCore.Extensions;
using LMCore.IO;
using LMCore.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public delegate void ReleasePlayerEvent();

public class ActiveOS : MonoBehaviour, IOnLoadSave
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

    [SerializeField, HelpBox("Hidden launchers get activated once you've completed the main game")]
    List<ActiveOSAppLauncher> hiddenLaunchers = new List<ActiveOSAppLauncher>();

    [SerializeField]
    ActiveOSAppLauncher helpApp;

    [SerializeField]
    ActiveOSAppLauncher tutorialApp;

    bool osActive;

    private void OnEnable()
    {
        AnomalyManager.OnSetDay += SetDay;
        StartPositionCustom.OnCapturePlayer += EnableOS;
        StartPositionCustom.OnReleasePlayer += DisableOS;
    }

    private void OnDisable()
    {
        AnomalyManager.OnSetDay += SetDay;
        StartPositionCustom.OnCapturePlayer -= EnableOS;
        StartPositionCustom.OnReleasePlayer -= DisableOS;
    }

    private void DisableOS(LMCore.Crawler.GridEntity player)
    {
        Debug.Log("ActiveOS: Disabled");
        pointer.enabled = false;
        Cursor.visible = false;
        osActive = false;
        osCam.gameObject.SetActive(false);
        transform.HideAllChildren();
    }

    private void EnableOS(LMCore.Crawler.GridEntity player)
    {
        Debug.Log("ActiveOS: Enabled");
        pointer.enabled = true;
        osActive = true;
        osCam.gameObject.SetActive(true);
        transform.ShowAllChildren();

        if (AnomalyManager.instance.WonGame)
        {
            foreach (var launcher in hiddenLaunchers)
            {
                launcher.gameObject.SetActive(true);
                if (launcher.AutoStart)
                {
                    launcher.OpenApp();
                }
            }
        } else
        {
            foreach (var launcher in hiddenLaunchers)
            {
                launcher.gameObject.SetActive(false);
                launcher.CloseApp();
            }

            if (AnomalyManager.instance.WeekNumber > 1 && AnomalyManager.instance.Weekday == Weekday.Monday)
            {
                Debug.Log("ActiveOS: Showing help");
                tutorialApp.CloseApp();
                helpApp.OpenApp();
            } else {
                Debug.Log($"ActiveOS: {AnomalyManager.instance.WeekNumber} {AnomalyManager.instance.Weekday}");
            }

        }

    }

    private void Start()
    {
        SetDay(AnomalyManager.instance.Weekday);
    }

    private void SetDay(Weekday day)
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

    public int OnLoadPriority => 1;

    public void OnLoad<T>(T save) where T : new()
    {
        SetDay(AnomalyManager.instance.Weekday);
        DisableOS(null);
    }
}
