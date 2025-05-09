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
    float ignoreInteractTime = 1f;

    [SerializeField]
    float releaseDiceAfter;

    [SerializeField]
    Crossfader crossfader;

    bool triggedContinue;

    public void TransitionToOffice(InputAction.CallbackContext context)
    {
        if (triggedContinue || Time.timeSinceLevelLoad < ignoreInteractTime) return;

        if (context.performed)
        {
            if (!diceReleased)
            {
                ReleaseDice();
            } else
            {

                triggedContinue = true;

                crossfader.FadeIn(SwapScenes, "You are free to try finding all anomalies", keepUIAfterFaded: true);
            }
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

        ReleaseDice();
    }

    void ReleaseDice()
    {
        dice.ShowAllChildren();
        diceReleased = true;
    }
}
