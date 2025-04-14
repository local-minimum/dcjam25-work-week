using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class ActiveOSApp : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI Title;

    public void CloseApp()
    {
        gameObject.SetActive(false);
    }

    public void OpenApp()
    {
        gameObject.SetActive(true);
        GetComponentInParent<ActiveOS>().FocusApp(this);
    }
}
