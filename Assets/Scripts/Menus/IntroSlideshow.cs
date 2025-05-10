using LMCore.Extensions;
using LMCore.IO;
using System.Collections.Generic;
using TMPro;
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

    [SerializeField]
    TextMeshProUGUI skipText;

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


        var keyHint = InputBindingsManager
            .InstanceOrResource("InputBindingsManager")
            .GetActiveActionHint(GamePlayAction.Interact);

        skipText.text = $"{keyHint} Skip";
    }

    public void Skip(InputAction.CallbackContext context)
    {
        if (!playing) return;

        if (context.performed)
        {
            if (finalizing)
            {
                finalizeTime = Time.timeSinceLevelLoad - 1f;
            }
            Progress();
        }
    }

    void PlayStep()
    {
        var instruction = steps[step];

        anim.SetTrigger(instruction.trigger);
        speaker.Stop();
        speaker.clip = instruction.clip;
        speaker.Play();

        step++;

        skipText.gameObject.SetActive(true);
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
            skipText.gameObject.SetActive(true);
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
