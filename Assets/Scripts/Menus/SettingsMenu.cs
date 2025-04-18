using UnityEngine;

public class SettingsMenu : MonoBehaviour
{
    public void Back()
    {
        gameObject.SetActive(false);
    }

    private void Start()
    {
        gameObject.SetActive(false);
    }
}
