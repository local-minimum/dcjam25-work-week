using TMPro;
using UnityEngine;
using LMCore.Extensions;

namespace LMCore.UI
{
    // TODO: Force on top of other canvases (at least in the root)
    public class Crossfader : MonoBehaviour
    {
        enum FadeType { FadedIn, FadedOut, FadeIn, FadeOut };

        [SerializeField]
        FadeType startWith = FadeType.FadedOut;

        [SerializeField]
        string startMessage;

        [SerializeField]
        TextMeshProUGUI messageUI;

        [SerializeField]
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
        }

        System.Action OnFaded;
        bool keepUIAfterFaded;

        public void FadeIn(System.Action onFaded = null, string fadeMessage = null, bool keepUIAfterFaded = false) =>
            Fade(fadeInTrigger, onFaded, fadeMessage, keepUIAfterFaded);

        public void FadeOut(System.Action onFaded = null, string fadeMessage = null) =>
            Fade(fadeOutTrigger, onFaded, fadeMessage);

        public void Fade(string trigger, System.Action onFaded = null, string fadeMessage = null, bool keepUIAfterFaded = false)
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

            anim.SetTrigger(trigger);
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
            callback?.Invoke();
        }
    }
}
