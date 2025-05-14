using UnityEngine;

public class ActiveOSAppLauncher : MonoBehaviour
{
    [SerializeField]
    ActiveOSApp app;

    [SerializeField]
    bool autoStart;
    public bool AutoStart => autoStart;

    public void ClickLauncher()
    {
        OpenApp();
    }

    bool initialStateSet;

    private void Start()
    {
        if (initialStateSet) return;

        if (autoStart)
        {
            OpenApp();
        } else
        {
            CloseApp();
        }
    }

    public void OpenApp()
    {
        initialStateSet = true;
        app.OpenApp();
    }

    public void CloseApp()
    {
        initialStateSet = true;
        app.CloseApp();
    }
}
