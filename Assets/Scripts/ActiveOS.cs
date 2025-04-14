using TMPro;
using UnityEngine;

public delegate void ReleasePlayerEvent();

public class ActiveOS : MonoBehaviour
{
    public static event ReleasePlayerEvent OnReleasePlayer;
    
    [SerializeField]
    TextMeshProUGUI DayField;

    private void OnEnable()
    {
        AnomalyManager.OnSetDay += AnomalyManager_OnSetDay;
    }

    private void OnDisable()
    {
        AnomalyManager.OnSetDay += AnomalyManager_OnSetDay;
    }

    private void Start()
    {
        AnomalyManager_OnSetDay(AnomalyManager.instance.Weekday);
    }

    private void AnomalyManager_OnSetDay(Weekday day)
    {
        DayField.text = day.ToString();
    }

    public void ReleasePlayer()
    {
        OnReleasePlayer?.Invoke();
    }
}
