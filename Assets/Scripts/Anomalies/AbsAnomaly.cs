using UnityEngine;

public abstract class AbsAnomaly : MonoBehaviour
{
    [SerializeField]
    string anomalyId = "reverse-clock";

    private void OnEnable()
    {
        AnomalyManager.OnSetAnomaly += AnomalyManager_OnSetAnomaly;
    }

    private void OnDisable()
    {
        AnomalyManager.OnSetAnomaly += AnomalyManager_OnSetAnomaly;
    }

    private void AnomalyManager_OnSetAnomaly(string id)
    {
        if (anomalyId == id && !string.IsNullOrEmpty(anomalyId))
        {
            SetAnomalyState();
        } else
        {
            SetNormalState();
        }
    }

    abstract protected void SetAnomalyState();

    abstract protected void SetNormalState();
}
