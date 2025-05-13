using LMCore.IO;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [System.Serializable]
    public class SubSettings
    {
        public Button sectionBtn;
        public List<GameObject> parts = new List<GameObject>();
        public List<Selectable> firstSelected = new List<Selectable>();
    }

    [SerializeField]
    List<SubSettings> subSettings = new List<SubSettings>();

    SubSettings activeSubSetting;

    [SerializeField]
    UnityEvent OnBack;

    [SerializeField, Header("Video")]
    Button FullscreenBtn;

    [SerializeField]
    Button WindowedBtn;

    [SerializeField]
    TextMeshProUGUI VSyncText;

    [SerializeField, Range(30, 244)]
    int targetFrameRate = 60;

    [SerializeField, Header("Gameplay")]
    Button SmoothTransitionBtn;

    [SerializeField]
    Button InstantTransitionBtn;

    [SerializeField]
    Button EasymodeBtn;

    [SerializeField]
    Button NormalmodeBtn;

    [SerializeField]
    TextMeshProUGUI MonologuesBtnText;

    [SerializeField]
    Button ClearAnomaliesBtn;

    [SerializeField]
    Button BalancedAnomaliesBtn;

    [SerializeField]
    Button SleuthyAnomaliesBtn;

    [SerializeField]
    Button GolferBtn;

    [SerializeField]
    Button StewardBtn;

    [SerializeField]
    Button ZealousBtn;

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
        if (activeSubSetting == null)
        {
            activeSubSetting = subSettings.First();
        }
        ShowActiveSubSettings();

        showing = true;

        gameObject.SetActive(true);
    }

    public void Back()
    {
        showing = false;
        gameObject.SetActive(false);
        OnBack?.Invoke();
    }


    void ShowActiveSubSettings()
    {
        Selectable preSelected = null;
        bool selected = false;
        if (activeSubSetting.firstSelected != null)
        {
            foreach (var item in activeSubSetting.firstSelected)
            {
                if (item.interactable)
                {
                    preSelected = item;
                    EventSystem.current.SetSelectedGameObject(item.gameObject);
                    selected = true;
                    break;
                }
            }
        }

        foreach (var sub in subSettings)
        {
            bool active = activeSubSetting == sub;
            sub.sectionBtn.interactable = !active;
            foreach (var part in sub.parts)
            {
                part.SetActive(active);
            }

            var nav = sub.sectionBtn.navigation;
            nav.selectOnRight = preSelected;
            sub.sectionBtn.navigation = nav;
        }


        if (!selected)
        {
            foreach (var subSetting in subSettings)
            {
                if (subSetting == activeSubSetting || subSetting.sectionBtn == null || !subSetting.sectionBtn.interactable) continue;

                EventSystem.current.SetSelectedGameObject(subSetting.sectionBtn.gameObject);
            }
        }
    }

    public void SetActiveSubSetting(Button btn)
    {
        var selected = subSettings.FirstOrDefault(s => s.sectionBtn == btn);
        if (selected != null)
        {
            activeSubSetting = selected;
            ShowActiveSubSettings();
        }
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
        // Video
        SyncVideoButtons(Screen.fullScreen);

        var vSync = WWSettings.VSync.Value;
        if (vSync != QualitySettings.vSyncCount)
        {
            QualitySettings.vSyncCount = vSync;
        }

        SyncVSync();

        // Audio
        SyncAudioUI(MixerGroup.Music, MusicVolume, MusicMutedUI);
        SyncAudioUI(MixerGroup.Effects, EffectsVolume, EffectsMutedUI);
        SyncAudioUI(MixerGroup.Dialogue, DialogueVolume, DialogueMutedUI);

        // Gameplay
        GameSettings.InstantMovement.OnChange += InstantMovement_OnChange;
        InstantMovement_OnChange(GameSettings.InstantMovement.Value);

        SyncEasyModeButtons();
        SyncMonologues();
        SyncAnomalyDifficulty();
        SyncManagerPersonality();
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
        if (instant)
        {
            EventSystem.current.SetSelectedGameObject(SmoothTransitionBtn.gameObject);
        } else
        {
            EventSystem.current.SetSelectedGameObject(InstantTransitionBtn.gameObject);
        }
    }

    public void SetFullscreen()
    {
        if (Screen.fullScreen) return;
        Screen.fullScreen = true;
        SyncVideoButtons(true);
        SetSelectedVideoBtn(true);
    }

    public void SetWindowed()
    {
        if (!Screen.fullScreen) return;
        Screen.fullScreen = false;
        SyncVideoButtons(false);
        SetSelectedVideoBtn(false);
    }

    void SetSelectedVideoBtn(bool fullscreen)
    {

        if (fullscreen)
        {
            EventSystem.current.SetSelectedGameObject(WindowedBtn.gameObject);
        } else
        {
            EventSystem.current.SetSelectedGameObject(FullscreenBtn.gameObject);
        }
    }

    void SyncVideoButtons(bool fullscreen)
    {
        WindowedBtn.interactable = fullscreen;
        FullscreenBtn.interactable = !fullscreen;
    }

    public void ToggleVSync()
    {
        if (QualitySettings.vSyncCount == 0)
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = targetFrameRate;
        } else
        {
            QualitySettings.vSyncCount = 0;
        }

        Debug.Log($"V-sync updated to {QualitySettings.vSyncCount}");

        SyncVSync();
    }

    void SyncVSync()
    {
        VSyncText.text = QualitySettings.vSyncCount > 0 ? "X" : "";
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

    public void SetEasyMode(bool easymode)
    {
        WWSettings.EasyMode.Value = easymode;
        SyncEasyModeButtons();
        if (easymode)
        {
            EventSystem.current.SetSelectedGameObject(NormalmodeBtn.gameObject);
        } else
        {
            EventSystem.current.SetSelectedGameObject(EasymodeBtn.gameObject);
        }
    }

    void SyncEasyModeButtons()
    {
        var easy = WWSettings.EasyMode.Value;
        EasymodeBtn.interactable = !easy;
        NormalmodeBtn.interactable = easy;
    }

    public void ToggleMonologues()
    {
        WWSettings.MonologueHints.Value = !WWSettings.MonologueHints.Value;
        SyncMonologues();
    }

    void SyncMonologues()
    {
        MonologuesBtnText.enabled = WWSettings.MonologueHints.Value;
    }

    public void SetAnomalyDifficulty(Button btn)
    {
        if (btn == ClearAnomaliesBtn)
        {
            WWSettings.AnomalyDifficulty.Value = AnomalyDifficulty.Clear;
        } else if (btn == BalancedAnomaliesBtn)
        {
            WWSettings.AnomalyDifficulty.Value = AnomalyDifficulty.Balanced;
        } else if (btn == SleuthyAnomaliesBtn)
        {
            WWSettings.AnomalyDifficulty.Value = AnomalyDifficulty.Sleuthy;
        } else
        {
            Debug.LogWarning($"SettingsMenu: Unexpected button call from {btn}");
        }

        SyncAnomalyDifficulty();
        switch (WWSettings.AnomalyDifficulty.Value)
        {
            case AnomalyDifficulty.Clear:
                EventSystem.current.SetSelectedGameObject(BalancedAnomaliesBtn.gameObject);
                break;
            case AnomalyDifficulty.Balanced:
                EventSystem.current.SetSelectedGameObject(ClearAnomaliesBtn.gameObject);
                break;
            case AnomalyDifficulty.Sleuthy:
                EventSystem.current.SetSelectedGameObject(BalancedAnomaliesBtn.gameObject);
                break;
        }
        
    }

    void SyncAnomalyDifficulty()
    {
        switch (WWSettings.AnomalyDifficulty.Value)
        {
            case AnomalyDifficulty.Clear:
                ClearAnomaliesBtn.interactable = false;
                BalancedAnomaliesBtn.interactable = true;
                SleuthyAnomaliesBtn.interactable = true;
                break;
            case AnomalyDifficulty.Balanced:
                ClearAnomaliesBtn.interactable = true;
                BalancedAnomaliesBtn.interactable = false;
                SleuthyAnomaliesBtn.interactable = true;
                break;
            case AnomalyDifficulty.Sleuthy:
                ClearAnomaliesBtn.interactable = true;
                BalancedAnomaliesBtn.interactable = true;
                SleuthyAnomaliesBtn.interactable = false;
                break;
        }
    }

    public void SetManagerPersonality(Button btn)
    {
        if (btn == GolferBtn)
        {
            WWSettings.ManagerPersonality.Value = ManagerPersonality.Golfer;
        } else if (btn == StewardBtn)
        {
            WWSettings.ManagerPersonality.Value = ManagerPersonality.Steward;
        } else if (btn == ZealousBtn)
        {
            WWSettings.ManagerPersonality.Value = ManagerPersonality.Zealous;
        } else
        {
            Debug.LogWarning($"SettingsMenu: Unexpected button call from {btn}");
        }

        SyncManagerPersonality();

        switch (WWSettings.ManagerPersonality.Value)
        {
            case ManagerPersonality.Golfer:
                EventSystem.current.SetSelectedGameObject(StewardBtn.gameObject);
                break;
            case ManagerPersonality.Steward:
                EventSystem.current.SetSelectedGameObject(GolferBtn.gameObject);
                break;
            case ManagerPersonality.Zealous:
                EventSystem.current.SetSelectedGameObject(StewardBtn.gameObject);
                break;
        }
    }

    void SyncManagerPersonality()
    {
        switch (WWSettings.ManagerPersonality.Value)
        {
            case ManagerPersonality.Golfer:
                GolferBtn.interactable = false;
                StewardBtn.interactable = true;
                ZealousBtn.interactable = true;
                break;
            case ManagerPersonality.Steward:
                GolferBtn.interactable = true;
                StewardBtn.interactable = false;
                ZealousBtn.interactable = true;
                break;
            case ManagerPersonality.Zealous:
                GolferBtn.interactable = true;
                StewardBtn.interactable = true;
                ZealousBtn.interactable = false;
                break;
        }
    }

}
