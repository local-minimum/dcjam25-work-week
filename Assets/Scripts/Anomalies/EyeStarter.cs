using UnityEngine;

public class EyeStarter : MonoBehaviour
{
    [SerializeField]
    Animator anim;

    float startTime;

    void OnEnable()
    {
        startTime = Time.timeSinceLevelLoad + Random.value * 2f;
    }

    void Update()
    {
        if (!anim.enabled && Time.timeSinceLevelLoad > startTime)
        {
            anim.enabled = true;
        }
    }
}
