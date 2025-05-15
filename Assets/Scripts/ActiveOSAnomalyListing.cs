using LMCore.UI;
using TMPro;
using UnityEngine;

public class ActiveOSAnomalyListing : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI DescriptionUI;

    [SerializeField]
    VirtualButton ActivateButton;

    [SerializeField]
    VirtualButton HintButton;

    AnomalyManager.CensuredAnomaly anomaly;

    public string AnomalyId => anomaly.id;

    bool selected;
    bool hinted;

    private void OnDisable()
    {
        hinted = false;
        selected = false;
        hinting = false;
    }

    public void Unselect()
    {
        selected = false;
        Sync();
    }

    public void IndicateSelected()
    {
        selected = true;
        Sync();
    }

    public void Select()
    {
        selected = true;
        var anomalies = GetComponentInParent<ActiveOSAnomalies>();
        anomalies.SelectAnomalyToActivate(this);

        Sync();
    }

    public void Sync(AnomalyManager.CensuredAnomaly anomaly)
    {
        this.anomaly = anomaly;

        HintButton.gameObject.SetActive(true);
        HintButton.Interactable = true;
        ActivateButton.Interactable = true;

        Sync();
    }

    bool hinting;

    void Sync()
    {
        DescriptionUI.text = hinting ? anomaly.hint : $"{(anomaly.horror ? "[HORROR] " : "")}{anomaly.name}";
        ActivateButton.GetComponentInChildren<TextMeshProUGUI>(true).text = selected ? "X" : "-";
    }

    public void ShowHint()
    {
        hinted = true;
        hinting = true;

        HintButton.gameObject.SetActive(false);

        Sync();

        nextHinting = Time.timeSinceLevelLoad + hintShowDuration;
    }

    [SerializeField]
    float hintShowDuration = 3f;

    float nextHinting;

    private void Update()
    {
        if (!hinted || Time.timeSinceLevelLoad < nextHinting) return;

        hinting = !hinting;
        Sync();

        nextHinting = Time.timeSinceLevelLoad + hintShowDuration;
    }
}
