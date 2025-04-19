using UnityEngine;

public class BBCamera : MonoBehaviour
{
    [SerializeField]
    float speed = 1.5f;

    [SerializeField]
    float initialDelay = 3f;

    float realtimeStart;
    private void Start()
    {
        realtimeStart = Time.realtimeSinceStartup + initialDelay;
    }

    private void Update()
    {
        if (BBFight.FightStatus != FightStatus.InProgress) return;

        if (Time.realtimeSinceStartup < realtimeStart) return;

        transform.position += Vector3.right * speed * Time.deltaTime;
    }
}
