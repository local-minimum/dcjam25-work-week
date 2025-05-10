using LMCore.Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NewGameAnomaliesSettings : MonoBehaviour
{
    [SerializeField]
    Button clearCard;

    [SerializeField]
    Button balancedCard;

    [SerializeField]
    Button sleuthyCard;

    [SerializeField]
    UnityEvent OnSelectDifficulty;

    void Start()
    {
        transform.HideAllChildren();
    }

    public void Show()
    {
        transform.ShowAllChildren();
        EventSystem.current.SetSelectedGameObject(balancedCard.gameObject);
    }

    public void SetDifficulty(Button selected)
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
