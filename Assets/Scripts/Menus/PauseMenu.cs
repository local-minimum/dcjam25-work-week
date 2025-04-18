using LMCore.Extensions;
using LMCore.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class PauseMenu : AbsMenu
{
    [SerializeField]
    SettingsMenu settings;

    [SerializeField]
    GameObject actionsButtons;

    [SerializeField]
    bool doSave = true;

    public override bool PausesGameplay => true;

    public override string MenuId => "pause-menu";

    private void Start()
    {
        Blur();
    }

    public void ShowSettings()
    {
        settings.gameObject.SetActive(true);
        actionsButtons.SetActive(false);
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
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
        // Application.Quit();
    }

    protected override void Focus()
    {
        transform.ShowAllChildren();
    }

    protected override void Blur()
    {
        if (unloaded) return;

        transform.HideAllChildren();
        settings.gameObject.SetActive(false);
    }
}
