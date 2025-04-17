using UnityEngine;

public class BBCamera : MonoBehaviour
{
    [SerializeField]
    float speed = 1.5f;

    private void Update()
    {
        transform.position += Vector3.right * speed * Time.deltaTime;
    }
}
