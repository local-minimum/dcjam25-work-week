using UnityEngine;
using UnityEngine.InputSystem;

public delegate void BBPlayerHealthEvent(int health);

public class BBPlayerController : MonoBehaviour
{
    public event BBPlayerHealthEvent OnHealthChange;

    [SerializeField]
    int startHealth = 3;

    [SerializeField]
    Animator anim;

    [SerializeField]
    Vector2 speed = Vector2.one;

    Vector2 direction;

    public int Health { get; private set; }

    [SerializeField]
    float dashSpeedFactor = 3f;

    [SerializeField]
    float dashDuration = 1f;
    [SerializeField]
    float dashCooldown = 2f;

    bool dashing;
    float dashTimeThreshold;

    private void Start()
    {
        Health = startHealth;
        OnHealthChange?.Invoke(Health);
    }

    void HandleWalk(InputAction.CallbackContext context, string cardinal)
    {
        if (context.performed)
        {
            anim.SetTrigger(cardinal);
            switch (cardinal)
            {
                case "North":
                    direction.y = 1;
                    break;

                case "South":
                    direction.y = -1;
                    break;

                case "West":
                    direction.x = -1;
                    break;

                case "East":
                    direction.x = 1;
                    break;

            }
        } else if (context.canceled)
        {
            switch (cardinal)
            {
                case "North":
                    direction.y = 0;
                    break;

                case "South":
                    direction.y = 0;
                    break;

                case "West":
                    direction.x = 0;
                    break;

                case "East":
                    direction.x = 0;
                    break;

            }

            if (direction == Vector2.zero)
            {
                anim.SetTrigger("Stand");
            }
        }
    }

    public void WalkNorth(InputAction.CallbackContext context) =>
        HandleWalk(context, "North");

    public void WalkSouth(InputAction.CallbackContext context) =>
        HandleWalk(context, "South");

    public void WalkWest(InputAction.CallbackContext context) =>
        HandleWalk(context, "West");

    public void WalkEast(InputAction.CallbackContext context) =>
        HandleWalk(context, "East");

    public void Dash(InputAction.CallbackContext context)
    {
        if (!context.performed || dashing || Time.timeSinceLevelLoad < dashTimeThreshold)
        {
            return;
        }

        dashing = true;
        checkInLetter = false;
        dashTimeThreshold = Time.timeSinceLevelLoad + dashDuration;
        anim.SetTrigger("Dash");
    }

    bool checkInLetter;

    private void Update()
    {
        if (dashing && Time.timeSinceLevelLoad > dashTimeThreshold)
        {
            dashing = false;
            dashTimeThreshold = Time.timeSinceLevelLoad + dashCooldown;
            anim.SetTrigger("NoDash");
            checkInLetter = true;
        }

        transform.position += (Vector3)(direction * speed) * Time.deltaTime * (dashing ? dashSpeedFactor : 1f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (dashing) return;

        if (collision.gameObject.CompareTag("Letter"))
        {
            Health = Mathf.Max(0, Health - 1);
            OnHealthChange?.Invoke(Health);
        } else if (collision.gameObject.CompareTag("BBFace"))
        {
            Health = 0;
            OnHealthChange?.Invoke(Health);
        }

        checkInLetter = false;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {

        if (checkInLetter)
        {
            OnTriggerEnter2D(collision);
            checkInLetter = false;
        }
    }
}
