using UnityEngine;

public class ActiveOSAppLauncher : MonoBehaviour
{
    [SerializeField]
    ActiveOSApp app;

    [SerializeField]
    bool autoStart;

    public void ClickLauncher()
    {
        app.OpenApp();
    }


    private void Start()
    {
        if (autoStart)
        {
            app.OpenApp();
        } else
        {
            app.CloseApp();
        }
    }

}
