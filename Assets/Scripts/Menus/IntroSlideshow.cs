using LMCore.Extensions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class IntroSlideshow : MonoBehaviour
{
    [SerializeField]
    AudioSource speaker;

    [SerializeField]
    Animator anim;

    [System.Serializable]
    struct IntroStep
    {
        public AudioClip clip;
        public string trigger;
    }

    [SerializeField]
    List<IntroStep> steps = new List<IntroStep>();


    [SerializeField]
    string finalTrigger;

    [SerializeField]
    float finalDuration = 2f;

    System.Action OnComplete;
    int step = 0;

    bool playing;

    public void Start()
    {
        transform.HideAllChildren();
    }

    public void Show(System.Action onCompleteCallback)
    {
        transform.ShowAllChildren();
        playing = true;
        OnComplete = onCompleteCallback;
        step = 0;
        PlayStep();
    }

    public void Skip(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Progress();
        }
    }

    void PlayStep()
    {
        var instruction = steps[step];

        anim.SetTrigger(instruction.trigger);
        speaker.PlayOneShot(instruction.clip);

        step++;
    }

    bool finalizing;
    float finalizeTime;
    

    void Progress()
    {
        if (step < steps.Count)
        {
            PlayStep();
        } else if (!finalizing)
        {
            anim.SetTrigger(finalTrigger);
            finalizing = true;
            finalizeTime = Time.timeSinceLevelLoad + finalDuration;
        } else if (Time.timeSinceLevelLoad > finalizeTime)
        {
            playing = false;
            OnComplete?.Invoke();
        }
    }

    private void Update()
    {
        if (!playing || speaker.isPlaying) return;

        Progress();

    }
}
