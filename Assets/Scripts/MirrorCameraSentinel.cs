using UnityEngine;

public class MirrorCameraSentinel : MonoBehaviour
{
    public bool VisibleToPlayer { get; private set; }

    private void OnBecameVisible()
    {
        VisibleToPlayer = true; 
    }

    private void OnBecameInvisible()
    {
        VisibleToPlayer = false;
    }
}
