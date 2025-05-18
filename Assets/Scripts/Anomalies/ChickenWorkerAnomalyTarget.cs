using LMCore.TiledDungeon;
using UnityEngine;

public class ChickenWorkerAnomalyTarget : MonoBehaviour
{
    public bool VisibleThroughWindow;


    private void OnValidate()
    {
        if (GetComponentInParent<TDDecoration>() == null)
        {
            Debug.LogError($"{name} must be part of a decoration");
        }
    }
}
