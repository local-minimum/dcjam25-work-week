using LMCore.Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NewGameManagerSettings : MonoBehaviour
{
    [SerializeField]
    Button golfer;

    [SerializeField]
    Button steward;

    [SerializeField]
    Button zealous;

    [SerializeField]
    UnityEvent OnSelectPersonality;

    void Start()
    {
        transform.HideAllChildren();    
    }

    public void Show()
    {
        transform.ShowAllChildren();
        EventSystem.current.SetSelectedGameObject(steward.gameObject);
    }

    public void SetPersonality(Button selected)
    {
        if (selected == golfer)
        {
            WWSettings.ManagerPersonality.Value = ManagerPersonality.Golfer;
        } else if (selected == steward)
        {
            WWSettings.ManagerPersonality.Value = ManagerPersonality.Steward;
        } else if (selected == zealous)
        {
            WWSettings.ManagerPersonality.Value = ManagerPersonality.Zealous;
        } else
        {
            Debug.LogError($"NewGame Manager callback got unexpected card: {selected}");
        }

        transform.HideAllChildren();
        OnSelectPersonality?.Invoke();
    }
}
