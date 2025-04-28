using UnityEngine;

public class BBCamera : MonoBehaviour
{
    [SerializeField]
    BBPlayerController player;

    [SerializeField]
    float speed = 1.5f;

    [SerializeField]
    float initialDelay = 3f;

    float realtimeStart;

    private void OnEnable()
    {
        player.OnPlayerReady += Player_OnPlayerReady;
    }

    private void OnDisable()
    {
        player.OnPlayerReady -= Player_OnPlayerReady;
    }

    bool started = false;
    private void Player_OnPlayerReady()
    {
        realtimeStart = Time.realtimeSinceStartup + initialDelay;
        started = true;
    }

    private void Update()
    {
        if (!started) return;

        if (BBFight.FightStatus != FightStatus.InProgress) return;

        if (Time.realtimeSinceStartup < realtimeStart) return;

        transform.position += Vector3.right * speed * Time.deltaTime;
    }
}
