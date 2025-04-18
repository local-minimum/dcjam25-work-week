using LMCore.Crawler;
using UnityEngine;

public class WeChickenAnomaly : AbsAnomaly
{
    [SerializeField]
    GameObject normalAvatar;

    [SerializeField]
    Animator normalAnimator;

    [SerializeField]
    string normalResting;

    [SerializeField]
    string normalMoving;

    [SerializeField]
    GameObject abnormalAvatar;

    [SerializeField]
    Animator abnormalAnimator;

    [SerializeField]
    string abnormalResting;

    [SerializeField]
    string abnormalMoving;

    protected override void OnEnableExtra()
    {
        GridEntity.OnMove += GridEntity_OnMove;
    }

    bool wasResting = true;

    private void GridEntity_OnMove(GridEntity entity)
    {
        if (entity.EntityType != GridEntityType.PlayerCharacter) return;

        if (activeAnomaly)
        {
            ToggleAnimator(entity, abnormalAnimator, abnormalResting, abnormalMoving);
        } else
        {
            ToggleAnimator(entity, normalAnimator, normalResting, normalMoving);
        }
    }

    void ToggleAnimator(
        GridEntity entity,
        Animator animator, 
        string restingTrigger, 
        string movingTrigger)
    {
        if (animator != null)
        {
            if (entity.Moving == MovementType.Stationary)
            {
                // Debug.Log($"WeChick {restingTrigger}");
                if (!wasResting)
                {
                    animator.SetTrigger(restingTrigger);
                    wasResting = true;
                }
            } else
            {
                // Debug.Log($"WeChick {movingTrigger}");
                if (wasResting)
                {
                    animator.SetTrigger(movingTrigger);
                    wasResting = false;
                }
            }
        }
    }

    protected override void OnDisableExtra()
    {
        GridEntity.OnMove -= GridEntity_OnMove;
    }

    bool activeAnomaly;

    protected override void SetAnomalyState()
    {
        activeAnomaly = true;
        if (abnormalAvatar != null) abnormalAvatar.SetActive(true);
        if (normalAvatar != null) normalAvatar.SetActive(false);
    }

    protected override void SetNormalState()
    {
        activeAnomaly = false;
        if (abnormalAvatar != null) abnormalAvatar.SetActive(false);
        if (normalAvatar != null) normalAvatar.SetActive(true);
    }
}
