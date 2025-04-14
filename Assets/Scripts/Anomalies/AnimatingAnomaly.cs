using UnityEngine;

public class AnimatingAnomaly : AbsAnomaly
{
    [SerializeField]
    GameObject anomalyRoot;

    [SerializeField]
    Animator animator;

    [SerializeField]
    string startTrigger;

    protected override void SetAnomalyState()
    {
        anomalyRoot.SetActive(true);
        if (animator != null)
        {
            Debug.Log($"Anomaly '{anomalyId}' triggers '{startTrigger}' on {animator}");
            animator.SetTrigger(startTrigger);
        }
    }

    protected override void SetNormalState()
    {
        Debug.Log($"Anomaly '{anomalyId}' deactivates {anomalyRoot}");
        anomalyRoot.SetActive(false);
    }
}
