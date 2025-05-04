using LMCore.Extensions;
using UnityEngine;

namespace LMCore.Juice
{
    [RequireComponent(typeof(AudioSource))]
    public class FadingSoundSource : MonoBehaviour
    {
        AudioSource _speaker;
        AudioSource speaker
        {
            get
            {
                if (_speaker == null)
                {
                    _speaker = GetComponent<AudioSource>();
                }
                return _speaker;
            }
        }

        bool baselineRegistered;
        float baselineVolume;

        [SerializeField, Range(0,1)]
        float fadedOutVolume = 0f;

        [SerializeField, Tooltip("0 is faded out volume, 1 is baseline volue, x-axis determines duration of fade")]
        AnimationCurve fadeIn;

        [SerializeField, Tooltip("0 is faded out volume, 1 is baseline volue, x-axis determines duration of fade")]
        AnimationCurve fadeOut;

        private void Start()
        {
            RecordBaseline();
        }

        void RecordBaseline()
        {
            if (baselineRegistered) return;

            baselineVolume = speaker.volume;
            baselineRegistered = true;
        }

        AnimationCurve activeFade;
        float fadeStartTime;
        float noSoundLevel;
        float fullSoundLevel;

        public void FadeIn() => FadeIn(null, null);
        public void FadeIn(float? fromValue, float? toValue)
        {
            RecordBaseline();
            activeFade = fadeIn;
            fadeStartTime = Time.realtimeSinceStartup;
            noSoundLevel = fromValue ?? fadedOutVolume;
            fullSoundLevel = toValue ?? baselineVolume;
        }

        public void FadeOut() => FadeOut(null);
        public void FadeOut(float? toValue)
        {
            RecordBaseline();
            activeFade = fadeOut;
            fadeStartTime = Time.realtimeSinceStartup;
            noSoundLevel = toValue ?? fadedOutVolume;
            fullSoundLevel = speaker.volume;
        }

        private void Update()
        {
            if (activeFade == null) return;

            var duration = activeFade.Duration();
            if (duration == 0f)
            {
                Debug.LogError($"FadingSoundSource {name}: Lacks duration in one of its animation curves");
                activeFade = null;
            }

            var progress = Mathf.Clamp01((Time.realtimeSinceStartup - fadeStartTime) / duration);
            speaker.volume = Mathf.Lerp(noSoundLevel, fullSoundLevel, activeFade.Evaluate(progress));

            if (progress == 1f)
            {
                activeFade = null;
            }
        }
    }
}
