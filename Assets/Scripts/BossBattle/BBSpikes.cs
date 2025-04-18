using UnityEngine;

public class BBSpikes : MonoBehaviour
{
    [SerializeField]
    Camera cam;

    [SerializeField]
    Transform back;

    [SerializeField]
    Transform front;

    [SerializeField]
    float magnitude = 1f;

    [SerializeField]
    float speed = 1f;

    [SerializeField]
    float screenXPos = 0.05f;

    void SyncPosition()
    {
        var worldPoint = cam.ScreenToWorldPoint(new Vector3(Screen.width * screenXPos, Screen.height / 2f), Camera.MonoOrStereoscopicEye.Mono);
        worldPoint.z = 0;
        transform.position = worldPoint;
    }

    void Update()
    {
        SyncPosition();

        Vector3 backPos = back.localPosition;
        backPos.y = Mathf.Sin(Time.timeSinceLevelLoad * speed) * magnitude;
        back.localPosition = backPos;

        Vector3 frontPos = front.localPosition;
        frontPos.y = Mathf.Cos(Time.timeSinceLevelLoad * speed) * magnitude;
        front.localPosition = frontPos;

    }
}
