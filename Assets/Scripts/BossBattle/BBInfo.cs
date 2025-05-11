using LMCore.IO;
using TMPro;
using UnityEngine;

public class BBInfo : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI Title;

    [SerializeField]
    TextMeshProUGUI Continue;

    private void OnEnable()
    {
        SyncHints();
        ActionMapToggler.OnChangeControls += ActionMapToggler_OnChangeControls;
    }

    private void OnDisable()
    {
        ActionMapToggler.OnChangeControls -= ActionMapToggler_OnChangeControls;
    }

    private void ActionMapToggler_OnChangeControls(UnityEngine.InputSystem.PlayerInput input, string controlScheme, LMCore.Extensions.SimplifiedDevice device)
    {
        SyncHints();
    }

    void SyncHints()
    {

        var keyHint = InputBindingsManager
            .InstanceOrResource("InputBindingsManager")
            .GetActiveActionHint(GamePlayAction.Interact);

        Title.text = $"Dash {keyHint}";
        Continue.text = $"Press {keyHint} to start";
    }
}
