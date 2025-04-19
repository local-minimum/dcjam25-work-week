using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(RectTransform))]
public class ActiveOSApp : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI Title;

    [SerializeField]
    UnityEvent OnShow;

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
        OnShow?.Invoke();
    }
}
