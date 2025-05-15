using LMCore.Extensions;
using LMCore.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ActiveOSAnomalies : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI FirstPageTextUI;

    [SerializeField]
    TextMeshProUGUI OtherPageTextUI;

    [SerializeField]
    int anomaliesOnFirstPage = 5;

    [SerializeField]
    int anomaliesOnOtherPages = 15;

    [SerializeField]
    VirtualButton previousButton;

    [SerializeField]
    VirtualButton nextButton;

    [SerializeField]
    VirtualButton forceNormalDayBtn;

    [SerializeField]
    VirtualButton letGameDecideBtn;

    [SerializeField]
    RectTransform dynamicContentParent;

    [SerializeField]
    ActiveOSAnomalyListing anomalyListingPrefab;

    List<ActiveOSAnomalyListing> listings;

    List<AnomalyManager.CensuredAnomaly> anomalies;
    int pageIdx = 0;

    [ContextMenu("Load Contents")]
    public void LoadApp()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Can only be done when application is running");
            return;
        }

        if (listings == null)
        {
            listings = GetComponentsInChildren<ActiveOSAnomalyListing>(true).ToList();
        }

        anomalies = AnomalyManager.instance.GetCensuredAnomalies().ToList();
        pageIdx = 0;

        SyncButtons();
        SyncText();

        forceNormalDayBtn.Interactable = false;
        letGameDecideBtn.Interactable = false;
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
        bool firstPage = pageIdx == 0;
        FirstPageTextUI.gameObject.SetActive(firstPage);
        OtherPageTextUI.gameObject.SetActive(!firstPage);

        DeactivateAllListings();
        foreach (var anomaly in anomalies.Skip(StartAnomalyIdxPage).Take(AnomaliesOnPage))
        {
            var item = listings.GetInactiveOrInstantiate(
                anomalyListingPrefab,
                dynamicContentParent);
            item.Sync(anomaly);

            if (item.AnomalyId == selectedListing)
            {
                item.IndicateSelected();
            }
        }
    }

    string selectedListing;

    public void SelectAnomalyToActivate(ActiveOSAnomalyListing listing)
    {
        forceNormalDayBtn.Interactable = true;
        letGameDecideBtn.Interactable = true;

        var id = listing.AnomalyId;
        UnselectSelected();

        selectedListing = listing.AnomalyId;
        AnomalyManager.instance.OverrideAnomalyOfTheDay(selectedListing);
    }

    void UnselectSelected()
    {
        if (selectedListing == null) return;

        foreach (var other in listings)
        {
            if  (selectedListing == other.AnomalyId)
            {
                other.Unselect();
                break;
            }
        }
    }

    public void ForceNormalDay()
    {
        forceNormalDayBtn.Interactable = false;
        letGameDecideBtn.Interactable = true;

        UnselectSelected();
        selectedListing = null;
        AnomalyManager.instance.OverrideAnomalyOfTheDay(null);
    }

    public void LetGameDecideAnomalyOrNot()
    {
        forceNormalDayBtn.Interactable = true;
        letGameDecideBtn.Interactable = false;
        UnselectSelected();
        selectedListing = null;
        AnomalyManager.instance.RemoveAnomalyOveride();
    }

    void DeactivateAllListings()
    {
        foreach (var listing in listings)
        {
            listing.gameObject.SetActive(false);
        }
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
