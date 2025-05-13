using LMCore.Extensions;
using LMCore.Juice;
using LMCore.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
    IntroSlideshow intro;

    [SerializeField]
    TextMeshProUGUI titleUI;

    [SerializeField]
    UnityEvent OnNewGame;

    [SerializeField]
    Crossfader crossfader;

    [SerializeField, Range(0, 1)]
    float introMusicLevel = 0.3f;

    [SerializeField]
    FadingSoundSource music;

    public void LoadSave()
    {
        Debug.Log("MainMenu: Loading game");
        music.FadeOut();
        WWSaveSystem.SafeInstance.LoadAutoSave();
    }

    public void NewGame()
    {
        Debug.Log("MainMenu: Starting new game");
        WWSaveSystem.SafeInstance.DeleteAutoSave();

        transform.HideAllChildren();
        titleUI.enabled = false;
        HideWipeWarning();

        OnNewGame?.Invoke();
    }

    public void ShowSlideshow()
    {
        music.FadeOut(toValue: introMusicLevel);

        Cursor.visible = false;

        intro.Show(FadeToOfficeScene);
    }

    void FadeToOfficeScene()
    {
        music.FadeOut();

        if (crossfader != null)
        {
            crossfader.FadeIn(SwapToOfficeScene, keepUIAfterFaded: true);
        } else
        {
            SwapToOfficeScene();
        }
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
        SettingsMenu.Show();
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

        Cursor.visible = true;

        var anomalyManager = AnomalyManager.instance;
        if (anomalyManager != null)
        {
            anomalyManager.ResetProgress();
        }
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
    bool showingWarning;

    public void ShowWipeWarning()
    {
        if (WWSaveSystem.instance.HasAutoSave)
        {
            PromptUI.instance.ShowText(wipeWarning);
            showingWarning = true;
        }
    }

    public void HideWipeWarning()
    {
        if (showingWarning)
        {
            PromptUI.instance.RemoveText(wipeWarning);
            showingWarning = false;
        }
    }
}
