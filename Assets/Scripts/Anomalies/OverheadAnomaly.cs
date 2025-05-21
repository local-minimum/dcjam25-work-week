using LMCore.Crawler;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OverheadAnomaly : AbsAnomaly
{
    [SerializeField]
    Vector3Int TriggerCoordinates;

    [SerializeField]
    AudioSource speaker;

    [SerializeField, Header("Walking")]
    Animator walkAnim;

    [SerializeField]
    string walkTrigger;

    [SerializeField]
    string idleTrigger;

    [SerializeField]
    Transform startCheckpoint;

    [SerializeField]
    List<Transform> checkpoints = new List<Transform>();

    [SerializeField]
    AudioClip landBig;

    [SerializeField]
    AudioClip langSmall;

    [SerializeField]
    float speed = 1;

    [SerializeField, Header("Talking")]
    Animator talkAnim;

    [SerializeField]
    string talkTrigger;

    [SerializeField]
    string silentTrigger;

    public void PlayLand(bool big)
    {
        if (big)
        {
            speaker.PlayOneShot(landBig);
        } else
        {
            speaker.PlayOneShot(langSmall);
        }
    }

    protected override void OnEnableExtra()
    {
    }

    protected override void OnDisableExtra()
    {
        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
    }


    protected override void SetAnomalyState()
    {
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
        triggered = false;
    }

    protected override void SetNormalState()
    {
        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;

        walkAnim.SetTrigger(idleTrigger);
        talkAnim.SetTrigger(silentTrigger);

        transform.position = startCheckpoint.position;
        transform.rotation = startCheckpoint.rotation;

        currentCheckpoint = startCheckpoint;
        targetCheckpoint = null;

        triggered = false;
    }

    bool triggered;

    private void GridEntity_OnPositionTransition(GridEntity entity)
    {
        if (entity.EntityType != GridEntityType.PlayerCharacter) return;

        bool triggering = !triggered;
        if (!triggered)
        {
            if (TriggerCoordinates != entity.Coordinates)
            {
                Debug.Log($"Overhead: Not triggering {TriggerCoordinates} != {entity.Coordinates}");
                return;
            }
            triggered = true;
            Debug.Log("Overhead: Triggered");
        } 

        var newTarget = checkpoints.OrderBy(pt => Vector3.SqrMagnitude(pt.position - entity.transform.position)).First();
        if (newTarget != currentCheckpoint)
        {
            Debug.Log("Overhead: Walking");
            targetCheckpoint = newTarget;
            startPosition = transform.position;
            talkAnim.SetTrigger(silentTrigger);
            walkAnim.SetTrigger(walkTrigger);
            distance = (targetCheckpoint.position - startPosition).magnitude;
            walkStart = Time.timeSinceLevelLoad;
            player = entity;
        } else if (triggering)
        {
            Debug.Log("Overhead: Talking");
            talkAnim.SetTrigger(talkTrigger);
        }
    }

    GridEntity player;
    Transform currentCheckpoint;
    Vector3 startPosition;
    Transform targetCheckpoint;
    float distance;
    float walkStart;

    private void Update()
    {
        if (targetCheckpoint == null) return;

        var lookDirection = player.transform.position - transform.position;
        lookDirection.y = 0;

        var targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 0.4f * Time.deltaTime);
        
        var progress = Mathf.Clamp01(speed * (Time.timeSinceLevelLoad - walkStart) / distance);
        transform.position = Vector3.Lerp(startPosition, targetCheckpoint.position, progress);

        if (progress == 1)
        {
            currentCheckpoint = targetCheckpoint;
            transform.rotation = targetRotation;
            talkAnim.SetTrigger(talkTrigger);
            walkAnim.SetTrigger(idleTrigger);
            targetCheckpoint = null;
            Debug.Log("Overhead: Talking");
        }
    }
}
