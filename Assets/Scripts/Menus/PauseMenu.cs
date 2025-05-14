using LMCore.Extensions;
using LMCore.IO;
using LMCore.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PauseMenu : AbsMenu
{
    [SerializeField]
    SettingsMenu settings;

    [SerializeField]
    GameObject actionsButtons;

    [SerializeField]
    GameObject preSelectedButton;

    [SerializeField]
    bool doSave = true;

    [SerializeField]
    Crossfader crossfader;

    public override bool PausesGameplay => true;

    public override string MenuId => "pause-menu";

    private void OnEnable()
    {
        ActionMapToggler.OnChangeControls += ActionMapToggler_OnChangeControls;
    }

    private void OnDisable()
    {
        ActionMapToggler.OnChangeControls -= ActionMapToggler_OnChangeControls;
    }

    private void ActionMapToggler_OnChangeControls(UnityEngine.InputSystem.PlayerInput input, string controlScheme, SimplifiedDevice device)
    {
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            EventSystem.current.SetSelectedGameObject(preSelectedButton);
        }
    }

    private void Start()
    {
        Blur();
    }

    public void ShowSettings()
    {
        settings.Show();
        actionsButtons.SetActive(false);
        Debug.Log($"PauseMenu: Showing settings {settings.gameObject.activeSelf} and hiding own buttons {actionsButtons.activeSelf}");
    }

    public void RegainFocus()
    {
        actionsButtons.SetActive(true);
        EventSystem.current.SetSelectedGameObject(preSelectedButton);
    }

    bool unloaded;
    public void SaveAndTitleScreen()
    {
        if (doSave)
        {
            WWSaveSystem.SafeInstance.AutoSave();
        }
        unloaded = true;
        if (crossfader != null)
        {
            crossfader.FadeIn(LoadTitleScene, keepUIAfterFaded: true);
        } else
        {
            LoadTitleScene();
        }
    }

    void LoadTitleScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
    }

    protected override void Focus()
    {
        transform.ShowAllChildren();
        Cursor.visible = true;
        EventSystem.current.SetSelectedGameObject(preSelectedButton);
    }

    protected override void Blur()
    {
        if (unloaded) return;

        transform.HideAllChildren();
        settings.gameObject.SetActive(false);
        Cursor.visible = false;
    }
}
