using LMCore.Extensions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public delegate void PlayerReadyEvent();

public delegate void BBPlayerHealthEvent(int health);

public class BBPlayerController : MonoBehaviour
{
    public event PlayerReadyEvent OnPlayerReady;
    public event BBPlayerHealthEvent OnHealthChange;

    [SerializeField]
    float initialNoInputTime = 1f;

    [SerializeField]
    BBInfo info;
    
    [SerializeField]
    AudioSource speaker;

    [SerializeField]
    List<AudioClip> HurtSounds = new List<AudioClip>();

    [SerializeField]
    Rigidbody2D rb;

    [SerializeField]
    int startHealth = 3;

    [SerializeField, Range(0, 1)]
    float startXScreenPos = 0.2f;

    [SerializeField]
    float startYScreenPos = 0.4f;

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

    [SerializeField, Range(0, 1)]
    float translationDeadAxisMin = 0.2f;

    float invulnUntil;

    public bool Dashing { get; private set; }
    float dashTimeThreshold;
    float dashEndTime;
    public float DashReadyProgress
    {
        get
        {
            if (Dashing) return 0f;
            if (Time.timeSinceLevelLoad > dashTimeThreshold) return 1f;
            return Mathf.Clamp01((Time.timeSinceLevelLoad - dashEndTime) / dashCooldown);
        }
    }

    private void Start()
    {
        Health = startHealth;
        OnHealthChange?.Invoke(Health);
        SetStartPosition();
    }

    [ContextMenu("Set start position")]
    void SetStartPosition()
    {
        var pos = transform.position;
        var target = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width * startXScreenPos, Screen.height * startYScreenPos));
        target.z = pos.z;
        transform.position = target; 
    }

    string lastAnimTrigger;
    void HandleWalk(InputAction.CallbackContext context, string cardinal)
    {
        if (Time.timeSinceLevelLoad < initialNoInputTime || info.gameObject.activeSelf) return;

        if (context.performed)
        {
            SetWalkTrigger(cardinal);

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
                    if (direction.y > 0) direction.y = 0;
                    break;

                case "South":
                    if (direction.y < 0) direction.y = 0;
                    break;

                case "West":
                    if (direction.x < 0) direction.x = 0;
                    break;

                case "East":
                    if (direction.x > 0) direction.x = 0;
                    break;

            }

            if (direction == Vector2.zero)
            {
                SetWalkTrigger("Stand");
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

    public void WalkTranslate(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            var value = context.ReadValue<Vector2>();
            if (Mathf.Abs(value.x) < translationDeadAxisMin)
            {
                value.x = 0f;
            }
            if (Mathf.Abs(value.y) < translationDeadAxisMin)
            {
                value.y = 0f;
            }

            direction = value;
            if (value.x != 0)
            {
                if (value.x > 0)
                {
                    SetWalkTrigger("East");
                } else
                {
                    SetWalkTrigger("West");
                }
            } else if (value.y != 0)
            {
                if (value.y > 0)
                {
                    SetWalkTrigger("North");
                } else
                {
                    SetWalkTrigger("South");
                }
                
            } else
            {
                SetWalkTrigger("Stand");
            }
        } else if (context.canceled)
        {
            direction = Vector2.zero;
            SetWalkTrigger("Stand");
        }
    }

    void SetWalkTrigger(string cardinal)
    {
        if (lastAnimTrigger != cardinal)
        {
            anim.SetTrigger(cardinal);
            lastAnimTrigger = cardinal;
        }
    }


    public void Dash(InputAction.CallbackContext context)
    {
        if (Time.timeSinceLevelLoad < initialNoInputTime) return;

        if (info.gameObject.activeSelf)
        {
            if (context.performed)
            {
                info.gameObject.SetActive(false);
                OnPlayerReady?.Invoke();
            }
            return;
        }

        if (!context.performed || Dashing || Time.timeSinceLevelLoad < dashTimeThreshold)
        {
            return;
        }

        Dashing = true;
        checkInLetter = false;
        dashTimeThreshold = Time.timeSinceLevelLoad + dashDuration;
        anim.SetTrigger("Dash");
    }

    bool checkInLetter;

    private void Update()
    {
        if (Dashing && Time.timeSinceLevelLoad > dashTimeThreshold)
        {
            Dashing = false;
            dashEndTime = Time.timeSinceLevelLoad;
            dashTimeThreshold = Time.timeSinceLevelLoad + dashCooldown;
            anim.SetTrigger("NoDash");
            checkInLetter = true;
        }

        rb.linearVelocity = (Vector3)(direction * speed) * (Dashing ? dashSpeedFactor : 1f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (Health == 0) return;

        if (collision.gameObject.CompareTag("Room"))
        {
            // Nothing to do but we still might be in a letter
            return;
        } else if (collision.gameObject.CompareTag("RoomWalls"))
        {
            // We should spawn next room please
            var room = collision.gameObject.GetComponentInParent<BBRoom>();
            if (room != null)
            {
                room.TriggerSpawnNextRoom();
            }
            return;
        } else if (collision.gameObject.CompareTag("Spikes"))
        {
            if (HurtSounds.Count > 0)
            {
                speaker.PlayOneShot(HurtSounds.GetRandomElement());
            }
            Health = 0;
            OnHealthChange?.Invoke(Health);
        }

        if (Dashing || Time.realtimeSinceStartup < invulnUntil) return;

        if (collision.gameObject.CompareTag("Letter"))
        {
            invulnUntil = Time.realtimeSinceStartup + invulnDuration;
            Health = Mathf.Max(0, Health - 1);
            if (HurtSounds.Count > 0)
            {
                speaker.PlayOneShot(HurtSounds.GetRandomElement());
            }
            OnHealthChange?.Invoke(Health);
        } else if (collision.gameObject.CompareTag("BBFace"))
        {
            Health = 0;
            if (HurtSounds.Count > 0)
            {
                speaker.PlayOneShot(HurtSounds.GetRandomElement());
            }
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
