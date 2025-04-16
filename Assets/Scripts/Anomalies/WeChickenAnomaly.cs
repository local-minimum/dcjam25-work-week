using LMCore.Crawler;
using UnityEngine;

public class WeChickenAnomaly : AbsAnomaly
{
    [SerializeField]
    GameObject normalAvatar;

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
            if (abnormalAnimator != null)
            {
                if (entity.Moving == MovementType.Stationary)
                {
                    Debug.Log($"WeChick {abnormalResting}");
                    if (!wasResting)
                    {
                        abnormalAnimator.SetTrigger(abnormalResting);
                        wasResting = true;
                    }
                } else
                {
                    Debug.Log($"WeChick {abnormalMoving}");
                    if (wasResting)
                    {
                        abnormalAnimator.SetTrigger(abnormalMoving);
                        wasResting = false;
                    }
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
