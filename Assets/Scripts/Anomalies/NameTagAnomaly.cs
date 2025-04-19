using TMPro;
using UnityEngine;

public class NameTagAnomaly : AbsAnomaly
{
    [SerializeField]
    TextMeshProUGUI Tag;

    [SerializeField]
    string NormalName;

    static string AbnormalName = "BOB";

    bool textSet = false;

    protected override void SetAnomalyState()
    {
        Tag.text = AbnormalName;
        textSet = true;
    }

    [ContextMenu("Set Normal Name")]
    protected override void SetNormalState()
    {
        Tag.text = NormalName;
        textSet = true;
    }

    protected override void OnEnableExtra()
    {
        if (!textSet)
        {
            Tag.text = NormalName;
        }
    }

    protected override void OnDisableExtra()
    {
    }

    private void Start()
    {
        OnEnableExtra();
    }
}
