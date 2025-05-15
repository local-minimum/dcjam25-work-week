using LMCore.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ActiveOSAnomalies : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI TextUI;

    [SerializeField, TextArea]
    string introText;

    [SerializeField]
    int anomaliesOnFirstPage = 5;

    [SerializeField]
    int anomaliesOnOtherPages = 15;

    [SerializeField]
    VirtualButton previousButton;

    [SerializeField]
    VirtualButton nextButton;

    List<AnomalyManager.CensuredAnomaly> anomalies;
    int pageIdx = 0;

    [ContextMenu("Load Contents")]
    public void LoadApp()
    {
        anomalies = AnomalyManager.instance.GetCensuredAnomalies().ToList();
        pageIdx = 0;

        SyncButtons();
        SyncText();
    }

    int StartAnomalyIdxPage => pageIdx == 0 ? 0 :
        anomaliesOnFirstPage + (pageIdx - 1) * anomaliesOnOtherPages;

    int AnomaliesOnPage => pageIdx == 0 ? anomaliesOnFirstPage : anomaliesOnOtherPages;

    int LastAnomalyIdxOnPageExclusive => StartAnomalyIdxPage + AnomaliesOnPage;


    void SyncButtons()
    {
        previousButton.Interactable = pageIdx > 0;
        nextButton.Interactable = LastAnomalyIdxOnPageExclusive < anomalies.Count; 
    }

    void SyncText()
    {
        var preamble = pageIdx == 0 ? $"{introText}\n\nAnomalies:\n\n" : "Anomalies Continued:\n\n";
        TextUI.text = $"{preamble}{string.Join("\n\n", anomalies.Skip(StartAnomalyIdxPage).Take(AnomaliesOnPage).Select(a => $"- {(a.horror ? "[HORROR] " : "")}{a.name}"))}";
    }

    public void NextPage()
    {
        pageIdx++;
        SyncButtons();
        SyncText();
    }

    public void PrevPage()
    {
        pageIdx--;
        SyncButtons();
        SyncText();
    }
}
