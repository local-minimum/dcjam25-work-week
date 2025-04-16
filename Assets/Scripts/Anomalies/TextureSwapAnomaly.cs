using UnityEngine;

public class TextureSwapAnomaly : AbsAnomaly
{
    [SerializeField]
    Renderer target;

    [SerializeField]
    Material normalMat;

    [SerializeField]
    Material anomalyMat;

    protected override void OnDisableExtra()
    {
    }

    protected override void OnEnableExtra()
    {
    }

    protected override void SetAnomalyState()
    {
        if (target != null && anomalyMat != null)
        {
            target.material = anomalyMat;
        }
    }

    protected override void SetNormalState()
    {
        if (target != null && normalMat != null)
        {
            target.material = normalMat;
        }
    }
}
