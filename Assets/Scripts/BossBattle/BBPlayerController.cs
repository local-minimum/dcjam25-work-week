using UnityEngine;
using UnityEngine.InputSystem;

public delegate void BBPlayerHealthEvent(int health);

public class BBPlayerController : MonoBehaviour
{
    public event BBPlayerHealthEvent OnHealthChange;

    [SerializeField]
    Rigidbody2D rb;

    [SerializeField]
    int startHealth = 3;

    [SerializeField, Range(0, 1)]
    float startXScreenPos = 0.2f;

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

    [SerializeField]
    float invulnDuration = 0.4f;

    float invulnUntil;

    bool dashing;
    float dashTimeThreshold;

    private void Start()
    {
        Health = startHealth;
        OnHealthChange?.Invoke(Health);
        var pos = transform.position;
        pos.x = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width * startXScreenPos, Screen.height / 2f)).x;
        transform.position = pos; 
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

        rb.linearVelocity = (Vector3)(direction * speed) * (dashing ? dashSpeedFactor : 1f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (dashing || Time.timeSinceLevelLoad < invulnUntil) return;

        if (collision.gameObject.CompareTag("Letter"))
        {
            invulnUntil = Time.timeSinceLevelLoad + invulnDuration;
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
