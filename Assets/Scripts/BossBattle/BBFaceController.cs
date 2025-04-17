using UnityEngine;

public class BBFaceController : MonoBehaviour
{
    [SerializeField]
    Camera cam;

    [SerializeField]
    Transform face;

    [SerializeField]
    Transform letterSpawnPoint;

    [SerializeField]
    Transform jaw;

    [SerializeField]
    Transform jawMaxOpen;

    [SerializeField]
    Transform faceMaxY;

    [SerializeField]
    Transform faceMinY;

    [SerializeField]
    float idleSpeed = 0.3f;

    bool idle = true;

    float idleSpeedDirection = 1;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    float yPosition = 0.5f;

    void SyncFacePosition()
    {
        var worldPoint = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height / 2f), Camera.MonoOrStereoscopicEye.Mono);
        var pos = transform.position;
        pos.x = worldPoint.x;
        pos.y = 0;
        transform.position = pos;

        pos.y = Vector3.Lerp(faceMinY.position, faceMaxY.position, yPosition).y;
        pos = transform.InverseTransformPoint(pos);
        pos.x = face.localPosition.x;
        face.localPosition = pos;
    }

    private void Update()
    {
        if (idle)
        {
            yPosition = Mathf.Clamp01(yPosition + idleSpeed * idleSpeedDirection * Time.deltaTime);

            if (yPosition == 0)
            {
                idleSpeedDirection = 1;
            } else if (yPosition == 1)
            {
                idleSpeedDirection = -1;
            }
        }

        SyncFacePosition();
    }
}
