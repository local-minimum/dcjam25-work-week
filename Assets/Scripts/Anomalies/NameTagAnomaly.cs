using TMPro;
using UnityEngine;

public class NameTagAnomaly : AbsAnomaly
{
    [SerializeField]
    TextMeshProUGUI Tag;

    [SerializeField]
    string NormalName;

    static string AbnormalName = "BOB";

    protected override void SetAnomalyState()
    {
        Tag.text = AbnormalName;
    }

    [ContextMenu("Set Normal Name")]
    protected override void SetNormalState()
    {
        Tag.text = NormalName;
    }

    protected override void OnEnableExtra()
    {
    }

    protected override void OnDisableExtra()
    {
    }
}
