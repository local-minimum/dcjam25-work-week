using UnityEngine;

public class OverheadAnomalyAnimEvents : MonoBehaviour
{
    public void BigLand()
    {
        GetComponentInParent<OverheadAnomaly>().PlayLand(true);
    }

    public void SmallLand()
    {
        GetComponentInParent<OverheadAnomaly>().PlayLand(true);
    }
}
