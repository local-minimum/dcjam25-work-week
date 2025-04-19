using LMCore.Extensions;
using LMCore.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class FinalScene : MonoBehaviour
{
    [SerializeField]
    Transform dice;

    [SerializeField]
    float releaseDiceAfter;

    [SerializeField]
    Crossfader crossfader;

    bool triggedContinue;

    public void TransitionToOffice(InputAction.CallbackContext context)
    {
        if (triggedContinue) return;

        if (context.performed)
        {
            triggedContinue = true;

            crossfader.FadeIn(SwapScenes, "You are free to try finding all anomalies", keepUIAfterFaded: true);
        }
    }

    void SwapScenes()
    {
        SceneManager.LoadScene("OfficeScene");
    }

    private void Start()
    {
        dice.HideAllChildren();
    }

    bool diceReleased = false;

    private void Update()
    {
        if (diceReleased || Time.timeSinceLevelLoad < releaseDiceAfter) return;

        dice.ShowAllChildren();
        diceReleased = true;


    }
}
