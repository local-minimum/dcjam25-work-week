using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    Button ContinueButton;

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
        ContinueButton.gameObject.SetActive(WWSaveSystem.SafeInstance.HasAutoSave);
    }
}
