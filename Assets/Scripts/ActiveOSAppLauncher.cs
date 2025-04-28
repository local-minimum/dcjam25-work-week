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

    private void Start()
    {
        if (autoStart)
        {
            OpenApp();
        } else
        {
            CloseApp();
        }
    }

    public void OpenApp() => app.OpenApp();

    public void CloseApp() => app.CloseApp();
}
