using TMPro;
using UnityEngine;
using LMCore.Extensions;
using UnityEngine.Events;

namespace LMCore.UI
{
    public class Crossfader : MonoBehaviour
    {
        [SerializeField]
        FadeType startWith = FadeType.FadedOut;

        [Tooltip("When the cross fader starts its animation to become visible")]
        public UnityEvent OnFadeInStart;

        [Tooltip("When the cross fader has become fully visible")]
        public UnityEvent OnFadeInEnd;

        [Tooltip("When the cross fader starts to remove itself")]
        public UnityEvent OnFadeOutStart;

        [Tooltip("When the cross fader has completly reomved itself")]
        public UnityEvent OnFadeOutEnd;

        private FadeType _state;
        public FadeType State
        {
            get => _state;
            set
            {
                _state = value;
                switch (value)
                {
                    case FadeType.FadeIn:
                        OnFadeInStart?.Invoke();
                        break;
                    case FadeType.FadedIn:
                        OnFadeInEnd?.Invoke();
                        break;
                    case FadeType.FadeOut:
                        OnFadeOutStart?.Invoke();
                        break;
                    case FadeType.FadedOut:
                        OnFadeOutEnd?.Invoke();
                        break;
                }
            }
        }

        public enum FadeType { FadedIn, FadedOut, FadeIn, FadeOut };

        [SerializeField, Header("Text")]
        string startMessage;

        [SerializeField]
        TextMeshProUGUI messageUI;

        [SerializeField, Header("Animations")]
        Animator anim;

        [SerializeField]
        string fadeInTrigger = "FadeIn";

        [SerializeField]
        string fadeOutTrigger = "FadeOut";

        [SerializeField]
        string fadedInTrigger = "FadedIn";

        [SerializeField]
        string fadedOutTrigger = "FadedOut";

        private void Start()
        {
            if (messageUI != null)
            {
                messageUI.text = startMessage;
            }

            if (startWith == FadeType.FadedOut)
            {
                if (anim.transform == transform)
                {
                    transform.HideAllChildren();
                } else
                {
                    anim.gameObject.SetActive(false);
                }

                anim.SetTrigger(fadedOutTrigger);
            } else
            {
                if (anim.transform == transform)
                {
                    transform.ShowAllChildren();
                } else
                {
                    anim.gameObject.SetActive(true);
                }

                if (startWith == FadeType.FadedIn)
                {
                    anim.SetTrigger(fadedInTrigger);
                } else if (startWith == FadeType.FadeIn)
                {
                    anim.SetTrigger(fadeInTrigger);
                } else if (startWith == FadeType.FadeOut)
                {
                    anim.SetTrigger(fadeOutTrigger);
                }
            }

            State = startWith;
        }

        System.Action OnFaded;
        bool keepUIAfterFaded;

        public void FadeIn(System.Action onFaded = null, string fadeMessage = null, bool keepUIAfterFaded = false) =>
            Fade(FadeType.FadeIn, onFaded, fadeMessage, keepUIAfterFaded);

        public void FadeOut(System.Action onFaded = null, string fadeMessage = null) =>
            Fade(FadeType.FadeOut, onFaded, fadeMessage);

        void Fade(FadeType fade, System.Action onFaded = null, string fadeMessage = null, bool keepUIAfterFaded = false)
        {
            this.keepUIAfterFaded = keepUIAfterFaded;

            if (OnFaded != null)
            {
                Debug.LogWarning("Crossfader: Calling previous on faded callback, it shouldn't have been any here");
                OnFaded.Invoke();
            }

            OnFaded = onFaded;

            if (messageUI != null)
            {
                messageUI.text = fadeMessage;
            }

            if (anim.transform == transform)
            {
                transform.ShowAllChildren();
            } else
            {
                anim.gameObject.SetActive(true);
            }

            switch (fade)
            {
                case FadeType.FadeIn:
                    anim.SetTrigger(fadeInTrigger);
                    break;
                case FadeType.FadeOut:
                    anim.SetTrigger(fadeOutTrigger);
                    break;
            }
            State = fade;
        }


        /// <summary>
        /// Set this as event in the fade in and out animations when they are completed 
        /// </summary>
        public void HandleFaded()
        {
            // So we don't stop clicks
            if (!keepUIAfterFaded)
            {
                if (anim.transform == transform)
                {
                    transform.HideAllChildren();
                } else
                {
                    anim.gameObject.SetActive(false);
                }
            }

            // Order might be important here in case the callback causes us to fade again!
            var callback = OnFaded;
            OnFaded = null;

            switch (State)
            {
                case FadeType.FadeIn:
                    State = FadeType.FadedIn;
                    break;
                case FadeType.FadeOut:
                    State = FadeType.FadedOut;
                    break;
            }

            callback?.Invoke();
        }
    }
}
