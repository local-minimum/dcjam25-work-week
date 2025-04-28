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
        var keyHint = InputBindingsManager
            .InstanceOrResource("InputBindingsManager")
            .GetActiveActionHint(GamePlayAction.Interact);

        Title.text = $"Dash {keyHint}";
        Continue.text = $"Press {keyHint} to start";
    }
}
