using LMCore.Extensions;
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
