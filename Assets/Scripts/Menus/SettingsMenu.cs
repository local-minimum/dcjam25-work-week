using LMCore.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField]
    UnityEvent OnBack;

    [SerializeField, Header("Video")]
    Button FullscreenBtn;

    [SerializeField]
    Button WindowedBtn;

    [SerializeField, Header("Gameplay")]
    Button SmoothTransitionBtn;

    [SerializeField]
    Button InstantTransitionBtn;

    [SerializeField, Header("Music")]
    Slider MusicVolume;
    [SerializeField]
    TextMeshProUGUI MusicMutedUI;

    [SerializeField]
    Slider EffectsVolume;
    [SerializeField]
    TextMeshProUGUI EffectsMutedUI;

    [SerializeField]
    Slider DialogueVolume;
    [SerializeField]
    TextMeshProUGUI DialogueMutedUI;

    bool showing;

    public void Show()
    {
        showing = true;
        gameObject.SetActive(true);
    }

    public void Back()
    {
        showing = false;
        gameObject.SetActive(false);
        OnBack?.Invoke();
    }


    private void Start()
    {
        if (!showing)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        GameSettings.InstantMovement.OnChange += InstantMovement_OnChange;
        InstantMovement_OnChange(GameSettings.InstantMovement.Value);

        SyncVideoButtons();

        SyncAudioUI(MixerGroup.Music, MusicVolume, MusicMutedUI);
        SyncAudioUI(MixerGroup.Effects, EffectsVolume, EffectsMutedUI);
        SyncAudioUI(MixerGroup.Dialogue, DialogueVolume, DialogueMutedUI);
    }

    private void OnDisable()
    {
        GameSettings.InstantMovement.OnChange -= InstantMovement_OnChange;
    }

    private void InstantMovement_OnChange(bool value)
    {
        InstantTransitionBtn.interactable = !value;
        SmoothTransitionBtn.interactable = value;
    }

    public void SetInstantMovement(bool instant) 
    {
        GameSettings.InstantMovement.Value = instant;
    }

    public void SetFullscreen()
    {
        if (Screen.fullScreen) return;
        Screen.fullScreen = true;
    }

    public void SetWindowed()
    {
        if (!Screen.fullScreen) return;
        Screen.fullScreen = false;
    }

    void SyncVideoButtons()
    {
        bool fullscreen = Screen.fullScreen;
        WindowedBtn.interactable = fullscreen;
        FullscreenBtn.interactable = !fullscreen;
    }

    [SerializeField]
    AudioMixer mixer;

    enum MixerGroup { Music, Effects, Dialogue }

    string GetGroupVolumeVariable(MixerGroup group)
    {
        switch (group)
        {
            case MixerGroup.Music:
                return "VolumeMusic";
            case MixerGroup.Effects:
                return "VolumeEffects";
            case MixerGroup.Dialogue:
                return "VolumeVoice";
            default:
                return null;
        }
    }

    float decibelToSlider(float db)
    {
        return Mathf.Clamp01((db + 80) / 80);
    }

    void SyncAudioUI(MixerGroup group, Slider slider, TextMeshProUGUI muted)
    {
        if (mixer.GetFloat(GetGroupVolumeVariable(group), out var db))
        {
            var value = decibelToSlider(db);
            slider.value = value;
            muted.enabled = value == 0;
        } else
        {
            Debug.LogWarning($"SettingsMenu: Could not read {group} volume");
        }
    }

    float SliderToDecibel(float slider) => 
        Mathf.Lerp(-80, 0, slider);

    public void SetMusicLevel(float sliderValue) =>
        SetAudioLevel(MixerGroup.Music, sliderValue, MusicMutedUI);

    public void SetEffectsLevel(float sliderValue) =>
        SetAudioLevel(MixerGroup.Effects, sliderValue, EffectsMutedUI);

    public void SetDialogueLevel(float sliderValue) =>
        SetAudioLevel(MixerGroup.Dialogue, sliderValue, DialogueMutedUI);

    void SetAudioLevel(MixerGroup group, float sliderValue, TextMeshProUGUI muted)
    {
        if (mixer.SetFloat(GetGroupVolumeVariable(group), SliderToDecibel(sliderValue)))
        {
            muted.enabled = sliderValue == 0;
        } else
        {
            Debug.LogWarning($"SettingsMenu: Could not set {group} to {sliderValue} slider value");
        }
    }
}
