using LMCore.Extensions;
using LMCore.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : AbsMenu
{
    [SerializeField]
    SettingsMenu settings;

    [SerializeField]
    GameObject actionsButtons;


    public override bool PausesGameplay => true;

    public override string MenuId => "pause-menu";

    private void Start()
    {
        Exit();
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

    public void SaveAndTitleScreen()
    {
        WWSaveSystem.SafeInstance.AutoSave();
        SceneManager.LoadScene("TitleScene");
    }

    protected override void Focus()
    {
        transform.ShowAllChildren();
    }

    protected override void Blur()
    {
        transform.HideAllChildren();
        settings.gameObject.SetActive(false);
    }
}
