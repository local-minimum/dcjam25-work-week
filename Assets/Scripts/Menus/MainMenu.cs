using LMCore.Extensions;
using LMCore.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    Button ResumeButton;

    [SerializeField]
    Button NewGameButton;

    [SerializeField]
    SettingsMenu SettingsMenu;

    [SerializeField]
    Crossfader crossfader;

    public void LoadSave()
    {
        WWSaveSystem.SafeInstance.LoadAutoSave();
    }

    public void NewGame()
    {
        WWSaveSystem.SafeInstance.DeleteAutoSave();
        // TODO: Do something nicer here!

        crossfader.FadeIn(SwapToOfficeScene, keepUIAfterFaded: true);
    }

    void SwapToOfficeScene()
    {
        SceneManager.LoadScene("OfficeScene");
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void Settings()
    {
        transform.HideAllChildren();
        SettingsMenu.gameObject.SetActive(true);
    }

    public void RegainFocus()
    {
        transform.ShowAllChildren();
        Start();
    }

    private void Start()
    {
        // Just in case, because there were nasty bugs with this before
        Time.timeScale = 1f;

        // This is because if we exit from the fight to main menu we would load the level wrong unless we
        // clean up its status;
        BBFight.FightStatus = FightStatus.None;
        BBFight.BaseDifficulty = 1;

        ResumeButton.gameObject.SetActive(WWSaveSystem.SafeInstance.HasAutoSave);
        SetDefaultSelectedButton();
    }

    void SetDefaultSelectedButton()
    {
        if (ResumeButton.gameObject.activeSelf)
        {
            EventSystem.current.SetSelectedGameObject(ResumeButton.gameObject);
        } else
        {
            EventSystem.current.SetSelectedGameObject(NewGameButton.gameObject);
        }
    }

    string wipeWarning = "This will erase your previous progress!";

    public void ShowWipeWarning()
    {
        if (WWSaveSystem.instance.HasAutoSave)
        {
            PromptUI.instance.ShowText(wipeWarning);
        }
    }

    public void HideWipeWarning()
    {
        if (WWSaveSystem.instance.HasAutoSave)
        {
            PromptUI.instance.RemoveText(wipeWarning);
        }
    }
}
