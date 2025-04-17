using TMPro;
using UnityEngine;

public class BBHealthUI : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI healthUI;

    [SerializeField]
    BBPlayerController player;

    private void OnEnable()
    {
        SetHealth(player.Health);
        player.OnHealthChange += SetHealth;
    }

    private void OnDisable()
    {
        player.OnHealthChange -= SetHealth;
    }

    void SetHealth(int health)
    {

        healthUI.text = string.Join(" ", System.Linq.Enumerable.Repeat("H", health));
    }
}
