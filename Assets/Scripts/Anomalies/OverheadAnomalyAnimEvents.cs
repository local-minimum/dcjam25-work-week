using System.Collections.Generic;
using UnityEngine;

public class OverheadAnomalyAnimEvents : MonoBehaviour
{
    public void BigLand()
    {
        if (!enabled) return;

        GetComponentInParent<OverheadAnomaly>().PlayLand(true);
    }

    public void SmallLand()
    {
        if (!enabled) return;

        GetComponentInParent<OverheadAnomaly>().PlayLand(true);
    }

    public void Talk()
    {
        if (!enabled) return;

        StartCoroutine(Words());
    }

    IEnumerator<WaitForSeconds> Words()
    {
        var anom = GetComponentInParent<OverheadAnomaly>();
        
        anom.Talk();
        yield return new WaitForSeconds(0.5f);
        anom.Talk();
        yield return new WaitForSeconds(0.5f);
        anom.Talk();
    }
}
