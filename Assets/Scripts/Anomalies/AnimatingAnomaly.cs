using System.Collections.Generic;
using UnityEngine;

public class AnimatingAnomaly : AbsAnomaly
{
    [SerializeField]
    GameObject anomalyRoot;

    [SerializeField]
    Animator animator;

    [SerializeField]
    string startTrigger;

    [SerializeField]
    string disableSiblingByName;

    [SerializeField]
    List<GameObject> disabledObjects = new List<GameObject>();

    protected override void OnDisableExtra()
    {
    }

    protected override void OnEnableExtra()
    {
    }

    protected override void SetAnomalyState()
    {
        anomalyRoot.SetActive(true);
        if (animator != null)
        {
            Debug.Log($"Anomaly '{anomalyId}' triggers '{startTrigger}' on {animator}");
            animator.SetTrigger(startTrigger);
        }
        ToggleSiblingByName(false);

        foreach (var obj in disabledObjects)
        {
            obj.SetActive(false);
        }
    }

    void ToggleSiblingByName(bool setActive)
    {
        if (!string.IsNullOrEmpty(disableSiblingByName))
        {
            var parent = transform.parent;
            for (int i = 0, n = parent.childCount; i<n;i++)
            {
                var sibing = parent.GetChild(i);
                if (sibing == transform) continue;
                if (sibing.name == disableSiblingByName)
                {
                    sibing.gameObject.SetActive(setActive);
                    break;
                }
            }
        }

        foreach (var obj in disabledObjects)
        {
            obj.SetActive(true);
        }
    }

    protected override void SetNormalState()
    {
        anomalyRoot.SetActive(false);
        ToggleSiblingByName(true);
    }
}
