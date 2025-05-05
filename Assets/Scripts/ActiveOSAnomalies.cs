using TMPro;
using UnityEngine;

public class ActiveOSAnomalies : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI TextUI;

    [ContextMenu("Load Contents")]
    public void LoadApp()
    {
        var anomalies = string.Join("\n\n", AnomalyManager.instance.GetCensuredAnomalies());
        TextUI.text = anomalies;
    }
}
