using UnityEngine;

public class WallClock : AbsAnomaly 
{
    [SerializeField]
    int startHour;

    [SerializeField]
    int startMinute;

    [SerializeField]
    int startSecond;

    [SerializeField]
    float gameTimeNormalScale = 1.5f;

    [SerializeField]
    float gameTimeAnomalyScale = -3f;

    [SerializeField]
    Transform hourHand;

    [SerializeField]
    Transform minuteHand;

    [SerializeField]
    Transform secondHand;

    [SerializeField]
    Vector3 eulerRotationAxis = Vector3.up;

    [SerializeField]
    Vector3 eulerRotationBase;

    float timeScale;

    protected override void OnDisableExtra()
    {
    }

    protected override void OnEnableExtra()
    {
    }

    protected override void SetAnomalyState()
    {
        timeScale = gameTimeAnomalyScale;
    }

    protected override void SetNormalState()
    {
        timeScale = gameTimeNormalScale;
    }

    float mod(float x, float m)
    {
        float r = x % m;
        return r < 0 ? r + m : r;
    }

    float PositiveProgress(float progress) => progress < 0 ? 1 + progress : progress;

    float ProgressToAngle(float progress)
    {
        return Mathf.Lerp(90, -270, progress);
    }

    private void Update()
    {
        var secondsSinceStart = Time.timeSinceLevelLoad * timeScale;
        var secondsProgress = PositiveProgress(mod(secondsSinceStart + startSecond, 60f) / 60f);
        var secondsAngle = ProgressToAngle(secondsProgress);
        secondHand.localEulerAngles = eulerRotationBase + eulerRotationAxis * secondsAngle;

        var minutesSinceStart = Mathf.Floor(secondsSinceStart / 60f);
        var minutes = mod(minutesSinceStart + startMinute, 60f);
        var minutesProgress = PositiveProgress(minutes / 60f);
        var minutesAngle = ProgressToAngle(minutesProgress);
        minuteHand.localEulerAngles = eulerRotationBase + eulerRotationAxis * minutesAngle;

        var hoursSinceStart = Mathf.Floor(minutesSinceStart / 60f);
        var amPmHours = mod(hoursSinceStart + startHour + minutes / 60f, 12f);
        var hoursProgress = PositiveProgress(amPmHours / 12f);
        var hoursAngle = ProgressToAngle(hoursProgress);
        hourHand.localEulerAngles = eulerRotationBase + eulerRotationAxis * hoursAngle;
    }
}
