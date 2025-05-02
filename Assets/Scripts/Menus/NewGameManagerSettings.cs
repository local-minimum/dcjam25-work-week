using LMCore.Extensions;
using LMCore.UI;
using UnityEngine;
using UnityEngine.Events;

public class NewGameManagerSettings : MonoBehaviour
{
    [SerializeField]
    UICard golfer;

    [SerializeField]
    UICard steward;

    [SerializeField]
    UICard zealous;

    [SerializeField]
    UnityEvent OnSelectPersonality;

    void Start()
    {
        transform.HideAllChildren();    
    }

    public void Show()
    {
        transform.ShowAllChildren();
    }

    public void SetPersonality(UICard selected)
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
