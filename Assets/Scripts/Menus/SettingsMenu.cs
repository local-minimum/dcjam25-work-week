using LMCore.IO;
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

    [SerializeField]
    Button SmoothTransitionBtn;

    [SerializeField]
    Button InstantTransitionBtn;

    public void Back()
    {
        gameObject.SetActive(false);
        OnBack?.Invoke();
    }


    private void Start()
    {
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GameSettings.InstantMovement.OnChange += InstantMovement_OnChange;
        InstantMovement_OnChange(GameSettings.InstantMovement.Value);
    }

    private void OnDisable()
    {
        GameSettings.InstantMovement.OnChange -= InstantMovement_OnChange;
    }

    private void InstantMovement_OnChange(bool value)
    {
        InstantTransitionBtn.interactable = !value;
        SmoothTransitionBtn.interactable = value;
    }

    public void SetInstantMovement(bool instant) 
    {
        GameSettings.InstantMovement.Value = instant;
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
