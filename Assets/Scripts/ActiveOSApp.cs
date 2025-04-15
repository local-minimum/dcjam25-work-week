using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class ActiveOSApp : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI Title;

    Vector2 resetAnchorPosition;

    private void OnEnable()
    {
        resetAnchorPosition = (transform as RectTransform).anchoredPosition;
    }

    private void OnDisable()
    {
        (transform as RectTransform).anchoredPosition = resetAnchorPosition;
    }

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
