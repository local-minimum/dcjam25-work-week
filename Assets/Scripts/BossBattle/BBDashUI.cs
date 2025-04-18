using System.Collections.Generic;
using UnityEngine;

public class BBDashUI : MonoBehaviour
{
    [SerializeField]
    BBPlayerController player;

    [SerializeField]
    List<GameObject> Letters = new List<GameObject>();

    private void Update()
    {
        var dashProgress = player.DashReadyProgress;
        var nLetters = Letters.Count;
        var showLetters = Mathf.FloorToInt(nLetters * dashProgress);

        for (int i = 0; i < nLetters; i++)
        {
            Letters[i].SetActive(i < showLetters);
        }
    }
}
