using LMCore.TiledDungeon.DungeonFeatures;
using UnityEngine;

public abstract class AbsAnomaly : TDFeature 
{
    [SerializeField]
    protected string anomalyId = "reverse-clock";

    private void OnEnable()
    {
        AnomalyManager.OnSetAnomaly += AnomalyManager_OnSetAnomaly;
        OnEnableExtra();
    }

    abstract protected void OnEnableExtra();

    private void OnDisable()
    {
        AnomalyManager.OnSetAnomaly -= AnomalyManager_OnSetAnomaly;
        OnDisableExtra();
    }

    abstract protected void OnDisableExtra();

    private void AnomalyManager_OnSetAnomaly(string id)
    {
        if (anomalyId == id && !string.IsNullOrEmpty(anomalyId))
        {
            Debug.Log($"Anomaly: '{anomalyId}' activated");
            SetAnomalyState();
        } else
        {
            // Debug.Log($"Anomaly: '{anomalyId}' not active because doing '{id}'");
            SetNormalState();
        }
    }

    abstract protected void SetAnomalyState();

    abstract protected void SetNormalState();

    private void OnDestroy()
    {
        OnDisable(); 
    }
}
