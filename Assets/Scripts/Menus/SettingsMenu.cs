using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField]
    UnityEvent OnBack;

    [SerializeField]
    Button FullscreenBtn;

    [SerializeField]
    Button WindowedBtn;

    public void Back()
    {
        gameObject.SetActive(false);
        OnBack?.Invoke();
    }

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void SetFullscreen()
    {
        if (Screen.fullScreen) return;
        Screen.fullScreen = true;
    }

    public void SetWindowed()
    {
        if (!Screen.fullScreen) return;
        Screen.fullScreen = false;
    }

    void SyncVideoButtons()
    {
        bool fullscreen = Screen.fullScreen;
        WindowedBtn.interactable = fullscreen;
        FullscreenBtn.interactable = !fullscreen;
    }
}
