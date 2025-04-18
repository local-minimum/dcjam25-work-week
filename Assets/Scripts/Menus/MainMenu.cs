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

    public void LoadSave()
    {
        WWSaveSystem.SafeInstance.LoadAutoSave();
    }

    public void NewGame()
    {
        WWSaveSystem.SafeInstance.DeleteAutoSave();
        // TODO: Do something nicer here!
        SceneManager.LoadScene("OfficeScene");
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void Settings()
    {
        // Sigh!
    }

    private void Start()
    {
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
}
