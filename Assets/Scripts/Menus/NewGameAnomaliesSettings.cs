using LMCore.Extensions;
using LMCore.UI;
using UnityEngine;
using UnityEngine.Events;

public class NewGameAnomaliesSettings : MonoBehaviour
{
    [SerializeField]
    UICard clearCard;

    [SerializeField]
    UICard balancedCard;

    [SerializeField]
    UICard sleuthyCard;

    [SerializeField]
    UnityEvent OnSelectDifficulty;

    void Start()
    {
        transform.HideAllChildren();
    }

    public void Show()
    {
        transform.ShowAllChildren();
    }

    public void SetDifficulty(UICard selected)
    {
        if (selected == clearCard)
        {
            WWSettings.AnomalyDifficulty.Value = AnomalyDifficulty.Clear;
        } else if (selected == balancedCard)
        {
            WWSettings.AnomalyDifficulty.Value = AnomalyDifficulty.Balanced;
        } else if (selected == sleuthyCard)
        {
            WWSettings.AnomalyDifficulty.Value = AnomalyDifficulty.Sleuthy;
        } else
        {
            Debug.LogError($"NewGame Anomalies callback got unexpected card: {selected}");
        }

        transform.HideAllChildren();
        OnSelectDifficulty?.Invoke();
    }

}
