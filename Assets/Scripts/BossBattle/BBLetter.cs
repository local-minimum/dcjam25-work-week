using UnityEngine;

public class BBLetter : MonoBehaviour
{
    Vector3 speed;

    private void OnBecameInvisible()
    {
        gameObject.SetActive(false);
        
    }

    public void SpitOut(Transform origin, Vector2 speed)
    {
        transform.position = origin.position;
        this.speed = speed;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        transform.position += speed * Time.deltaTime;
    }
}
