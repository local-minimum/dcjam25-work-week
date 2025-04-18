using UnityEngine;
using UnityEngine.Events;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField]
    UnityEvent OnBack;

    public void Back()
    {
        gameObject.SetActive(false);
        OnBack?.Invoke();
    }

    private void Start()
    {
        gameObject.SetActive(false);
    }
}
