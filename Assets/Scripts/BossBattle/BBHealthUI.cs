using System.Collections.Generic;
using UnityEngine;

public class BBHealthUI : MonoBehaviour
{
    [SerializeField]
    List<GameObject> hearts = new List<GameObject>();

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
        for (int i = 0, l = hearts.Count; i<l;i++)
        {
            hearts[i].SetActive(i < health);
        }
    }
}
