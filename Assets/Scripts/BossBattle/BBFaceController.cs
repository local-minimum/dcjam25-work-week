using LMCore.Extensions;
using System.Collections.Generic;
using UnityEngine;


public delegate void StartSpittingEvent();

public class BBFaceController : MonoBehaviour
{
    public static event StartSpittingEvent OnStartSpitting;

    [SerializeField]
    AudioSource speaker;

    [SerializeField]
    List<AudioClip> SpitSounds = new List<AudioClip>();

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
    float slideSpeed = 0.3f;

    bool sliding = true;

    float slideSpeedDirection = 1;

    [SerializeField]
    BBAlphabet alphabet;

    [SerializeField]
    Vector2 spitSpeed = new Vector2(-0.25f, 0f);

    [SerializeField]
    List<string> words = new List<string>() { "WORK" };

    [SerializeField]
    float letterPause = 0.2f;

    [SerializeField]
    float wordPause = 1.25f;

    [SerializeField]
    float mouthOpenDuration = 0.3f;

    [SerializeField]
    float mouthCloseDuration = 0.2f;

    float mouthStart;
    float mouthDuration;
    bool opening;

    
    float nextSpit = 5f;
    bool wording = false;
    int letterIdx = 0;
    string activeWord;

    float yPosition = 0.5f;

    bool firstSpit = true;

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

    private void Start()
    {
        opening = true;
        mouthDuration = mouthOpenDuration;
        mouthStart = nextSpit - mouthDuration;
    }

    private void Update()
    {
        if (sliding)
        {
            yPosition = Mathf.Clamp01(yPosition + slideSpeed * slideSpeedDirection * Time.deltaTime);

            if (yPosition == 0)
            {
                slideSpeedDirection = 1;
            } else if (yPosition == 1)
            {
                slideSpeedDirection = -1;
            }
        }

        SyncFacePosition();

        if (BBFight.FightStatus != FightStatus.InProgress) return;

        if (wording)
        {
            if (Time.timeSinceLevelLoad > nextSpit)
            {
                var letter = activeWord.Substring(letterIdx, 1);
                var bbLetter = alphabet.Get(letter);
                if (bbLetter != null)
                {
                    speaker.PlayOneShot(SpitSounds.GetRandomElement());
                    bbLetter.SpitOut(letterSpawnPoint, spitSpeed);
                }
                letterIdx++;

                if (letterIdx >= activeWord.Length)
                {
                    letterIdx = 0;
                    wording = false;
                    nextSpit = Time.timeSinceLevelLoad + wordPause;

                    mouthDuration = mouthCloseDuration;
                    mouthStart = Time.timeSinceLevelLoad;
                    opening = false;
                } else
                {
                    nextSpit = Time.timeSinceLevelLoad + letterPause;
                }
            }
        } else if (Time.timeSinceLevelLoad > nextSpit)
        {
            // TODO: Difficulty would make this funnier

            activeWord = words.GetRandomElementOrDefault();
            letterIdx = 0;
            wording = true;

            if (firstSpit)
            {
                OnStartSpitting?.Invoke();
                firstSpit = false;
            }

        }

        var mouthProgress = Mathf.Clamp01((Time.timeSinceLevelLoad - mouthStart) / mouthDuration);
        if (mouthProgress > 0f)
        {
            var closed = Vector3.zero;
            var opened = jawMaxOpen.localPosition;

            if (opening)
            {
                var pos = jaw.localPosition;
                pos.y = Vector3.Lerp(closed, opened, mouthProgress).y;
                jaw.localPosition = pos;
            } else
            {
                var pos = jaw.localPosition;
                pos.y = Vector3.Lerp(opened, closed, mouthProgress).y;
                jaw.localPosition = pos;
                if (mouthProgress == 1)
                {
                    mouthDuration = mouthOpenDuration;
                    mouthStart = nextSpit - mouthDuration;
                    opening = true;
                }
            }
        }
    }
}
