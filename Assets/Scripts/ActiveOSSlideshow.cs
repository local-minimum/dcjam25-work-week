using LMCore.Extensions;
using LMCore.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ActiveOSSlideshow : MonoBehaviour
{
    [System.Serializable]
    private class SlideData
    {
        [TextArea]
        public string description;
        public List<Sprite> sprites = new List<Sprite>();
    }

    [SerializeField, Header("Parts")]
    Image image;

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
            image.sprite = noImageFallback;
            text.text = noSlideText;
            spriteIndex = -1;
        } else
        {
            image.sprite = slide.sprites.FirstOrDefault()  ?? noImageFallback;
            // -1 index means no need to cycle them
            spriteIndex = slide.sprites.Count > 1 ? 0 : -1;
            nextSpriteTime = Time.timeSinceLevelLoad + showSpriteTime;
            text.text = slide.description;
        }

        image.enabled = image.sprite != null;
    }

    int LastIndex => slides.Count - 1;

    public void NextSlide()
    {
        var lastIndex = LastIndex;

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
        NextButton.Interactable = slideIndex < LastIndex;
        PreviousButton.Interactable = slideIndex > 0;
    }

    private void Update()
    {
        if (spriteIndex < 0 || slide == null || Time.timeSinceLevelLoad < nextSpriteTime) return;

        spriteIndex++;
        if (spriteIndex > LastIndex)
        {
            spriteIndex = 0;
        }

        image.sprite = slide.sprites.GetNthOrDefault(spriteIndex, noImageFallback);
        image.enabled = image.sprite != null;

        nextSpriteTime = Time.timeSinceLevelLoad + showSpriteTime;
    }
}
