using UnityEngine;

public class ActiveOSAppLauncher : MonoBehaviour
{
    [SerializeField]
    ActiveOSApp app;

    public void ClickLauncher()
    {
        app.OpenApp();
    }
}
