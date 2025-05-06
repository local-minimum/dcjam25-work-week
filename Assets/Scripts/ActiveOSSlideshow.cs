using LMCore.Extensions;
using LMCore.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActiveOSSlideshow : MonoBehaviour
{
    [System.Serializable]
    private struct Illustration
    {
        public Sprite baseColor;
        public Sprite secondColor;
        public Sprite thirdColor;
    }

    [System.Serializable]
    private class SlideData
    {
        [TextArea]
        public string description;
        public List<Illustration> illustrations = new List<Illustration>();
    }

    [SerializeField, Header("Parts")]
    Image IllustrationBaseColor;
    [SerializeField]
    Image IllustrationSecondColor;
    [SerializeField]
    Image IllustrationThirdColor;

    [SerializeField]
    TextMeshProUGUI text;

    [SerializeField]
    VirtualButton PreviousButton;

    [SerializeField]
    VirtualButton NextButton;

    [SerializeField, Header("Slides")]
    List<SlideData> slides = new List<SlideData>();

    int slideIndex;

    [SerializeField]
    Sprite noImageFallback;

    [SerializeField, TextArea]
    string noSlideText;

    [SerializeField]
    float showSpriteTime = 1f;

    private void Start()
    {
        SyncSlide();
        SyncButtons();
    }

    public void OnFocus()
    {
        SyncSlide();
        SyncButtons();
    }

    int spriteIndex;
    float nextSpriteTime;
    SlideData slide;

    [ContextMenu("Sync Slide")]
    void SyncSlide()
    {
        slide = slides.GetNthOrDefault(slideIndex, null);

        if (slide == null)
        {
            SetNoIllustration();
            text.text = noSlideText;
            spriteIndex = -1;
        } else
        {
            if (slide.illustrations.Count == 0)
            {
                SetNoIllustration();
            } else
            {
                SetIllustration(slide.illustrations.First());
            }
            // -1 index means no need to cycle them
            spriteIndex = slide.illustrations.Count > 1 ? 0 : -1;
            nextSpriteTime = Time.timeSinceLevelLoad + showSpriteTime;

            text.text = slide.description;
        }
    }

    void SetIllustration(Illustration illustration)
    {
        IllustrationBaseColor.sprite = illustration.baseColor;
        IllustrationSecondColor.sprite = illustration.secondColor;
        IllustrationThirdColor.sprite = illustration.thirdColor;

        IllustrationBaseColor.enabled = illustration.baseColor != null;
        IllustrationSecondColor.enabled = illustration.secondColor != null;
        IllustrationThirdColor.enabled = illustration.thirdColor != null;
    }

    void SetNoIllustration()
    {
        IllustrationBaseColor.sprite = noImageFallback;
        IllustrationSecondColor.sprite = null;
        IllustrationThirdColor.sprite = null;

        IllustrationBaseColor.enabled = noImageFallback != null;
        IllustrationSecondColor.enabled = false;
        IllustrationThirdColor.enabled = false;
    }

    int LastSlideIndex => slides.Count - 1;
    int LastIllustrationIndex => slide.illustrations.Count - 1;

    public void NextSlide()
    {
        var lastIndex = LastSlideIndex;

        slideIndex = Mathf.Min(slideIndex + 1, lastIndex);

        SyncSlide();
        SyncButtons();
    }

    public void PreviousSlide()
    {
        slideIndex = Mathf.Max(slideIndex - 1, 0);
        SyncSlide();
        SyncButtons();
    }


    void SyncButtons()
    {
        NextButton.Interactable = slideIndex < LastSlideIndex;
        PreviousButton.Interactable = slideIndex > 0;
    }

    private void Update()
    {
        if (spriteIndex < 0 || slide == null || Time.timeSinceLevelLoad < nextSpriteTime) return;

        spriteIndex++;
        if (spriteIndex > LastIllustrationIndex)
        {
            spriteIndex = 0;
        }

        if (spriteIndex >= 0 && spriteIndex <= LastIllustrationIndex)
        {
            SetIllustration(slide.illustrations[spriteIndex]);
        }

        nextSpriteTime = Time.timeSinceLevelLoad + showSpriteTime;
    }
}
