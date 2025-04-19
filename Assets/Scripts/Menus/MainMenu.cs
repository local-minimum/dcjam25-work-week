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
    IntroSlideshow intro;

    [SerializeField]
    AudioSource musicPlayer;

    [SerializeField]
    AnimationCurve easeDownMusic;

    [SerializeField]
    float easeDownMusicDuration;

    public void LoadSave()
    {
        WWSaveSystem.SafeInstance.LoadAutoSave();
    }

    bool lowerMusic;
    float lowerMusicStartTime;

    public void NewGame()
    {
        WWSaveSystem.SafeInstance.DeleteAutoSave();
        Cursor.visible = false;
        lowerMusic = true;
        lowerMusicStartTime = Time.timeSinceLevelLoad;
        intro.Show(SwapToOfficeScene);
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

        Cursor.visible = true;

        var anomalyManager = AnomalyManager.instance;
        if (anomalyManager != null)
        {
            anomalyManager.ResetProgress();
        }
    }

    private void Update()
    {
        if (!lowerMusic) return;

        var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - lowerMusicStartTime) / easeDownMusicDuration);
        musicPlayer.volume = easeDownMusic.Evaluate(progress);
        if (progress == 1f)
        {
            lowerMusic = false;
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
