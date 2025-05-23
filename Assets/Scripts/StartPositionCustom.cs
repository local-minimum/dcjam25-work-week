using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.IO;
using LMCore.TiledDungeon;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledDungeon.Enemies;
using LMCore.TiledImporter;
using LMCore.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public delegate void StartPositionPlayerEvent(GridEntity player);

public class StartPositionCustom : TDFeature, ITDCustom
{
    public static event StartPositionPlayerEvent OnReleasePlayer;
    public static event StartPositionPlayerEvent OnCapturePlayer;

    static bool notShownPauseHintToday = true;

    [SerializeField, Header("Audio Hints")]
    AudioSource speaker;

    [SerializeField]
    List<AudioClip> badFireEscapes = new List<AudioClip>();

    [SerializeField]
    List<AudioClip> badMainExits = new List<AudioClip>();

    [SerializeField]
    List<AudioClip> badBossExits = new List<AudioClip>();

    [SerializeField, Header("Transition")]
    Transform cameraPosition;

    [SerializeField]
    Movement releaseMovement = Movement.Backward;

    public void Configure(TDNode node, TiledCustomProperties properties)
    {
    }

    private void OnEnable()
    {
        // Hopefully this is good enough place to ensure this isn't funky
        Time.timeScale = 1f;
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
        ActiveOS.OnReleasePlayer += ReleasePlayer;
        ActionMapToggler.OnChangeControls += ActionMapToggler_OnChangeControls;
    }

    private void OnDisable()
    {
        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
        ActiveOS.OnReleasePlayer -= ReleasePlayer;
        ActionMapToggler.OnChangeControls -= ActionMapToggler_OnChangeControls;
    }

    private void ActionMapToggler_OnChangeControls(UnityEngine.InputSystem.PlayerInput input, string controlScheme, SimplifiedDevice device)
    {
        SyncPauseSettingsHint();
    }

    GridEntity player;
    FreeLookCamera freeLookCamera;
    Transform capturedCamera;
    Transform capturedCameraOriginalParent;
    Vector3 capturedCameraOriginalLocalPosition;
    Quaternion capturedCameraOriginalLocalRotation;

    private void GridEntity_OnPositionTransition(GridEntity entity)
    {
        if (entity.EntityType != GridEntityType.PlayerCharacter || 
            entity.Coordinates != Coordinates || 
            player != null) return;

        player = entity;

        entity.MovementBlockers.Add(this);
        freeLookCamera = entity.GetComponentInChildren<FreeLookCamera>(true);
        if (freeLookCamera != null)
        {
            freeLookCamera.enabled = false;
            // freeLookCamera.SetTranslationForwardOverride();
            capturedCamera = freeLookCamera.transform;

            Debug.Log($"StartPosition: Captured free looking camera {freeLookCamera}");
        } else
        {
            var cam = entity.GetComponentInChildren<Camera>(true);
            if (cam == null)
            {
                Debug.LogError($"StartPosition: Entit {entity} lacks a camera");
                return;
            }

            Debug.Log($"StartPosition: Captured free actual camera {cam}");
            capturedCamera = cam.transform;
        }

        if (capturedCamera != null)
        {
            capturedCameraOriginalLocalPosition = capturedCamera.localPosition;
            capturedCameraOriginalLocalRotation = capturedCamera.localRotation;
            capturedCameraOriginalParent = capturedCamera.parent;
            capturedCamera.SetParent(cameraPosition);
            capturedCamera.localPosition = Vector3.zero;
            capturedCamera.localRotation = Quaternion.identity;
        }

        var save = WWSaveSystem.ActiveSaveData;
        if (save == null)
        {
            Debug.Log("CustomStartPos: Today is the first day");
        } else if (save.anomalies.previousDayOutcome == AnomalyManager.PreviousDayOutcome.Negative)
        {
            switch (save.anomalies.previousDayExit)
            {
                case ExitType.FireEscape:
                    // Prompt some I guess yesterday was normal
                    PlayRandomHint(badFireEscapes);
                    break;
                case ExitType.MainExit:
                    // Prompt I must have missed something
                    PlayRandomHint(badMainExits);
                    break;
                case ExitType.BossDeath:
                    // Prompt avoid boss
                    PlayRandomHint(badBossExits);
                    break;
            }
        }

        OnCapturePlayer?.Invoke(player);
    }

    void PlayRandomHint(List<AudioClip> options)
    {
        if (!WWSettings.MonologueHints.Value) return;

        if (speaker == null)
        {
            Debug.LogError("CustomStartPos: Missing speaker");
            return;
        }

        if (options == null || options.Count == 0)
        {
            Debug.LogWarning("CustomStartPos: No optoins avaialbe");
            return;
        }

        var clip = options.GetRandomElement();
        speaker.PlayOneShot(clip);
    }

    void Start()
    {
        if (player == null)
        {
            if (Dungeon.Player.Coordinates == Coordinates)
            {
                Debug.Log("CustomStartPos: We failed to capture the player even though it's here so we're doing that manually");
                GridEntity_OnPositionTransition(Dungeon.Player);
            }
        }
    }

    [ContextMenu("Release player")]
    void ReleasePlayer()
    {
        releasing = true;
        releaseStart = Time.timeSinceLevelLoad;

        AnomalyManager.instance.PrepareAnomalyOrNormalDay();
    }

    [SerializeField]
    float showPauseHintDuration = 3f;

    void CompleteReleasePlayer()
    {
        if (freeLookCamera != null)
        {
            // freeLookCamera.RemoveTranslationForwardOverride();
            freeLookCamera.enabled = true;
        }

        if (player != null)
        {
            player.MovementBlockers.Remove(this);
        }

        if (capturedCamera != null)
        {
            capturedCamera.SetParent(capturedCameraOriginalParent);
            capturedCamera.localPosition = capturedCameraOriginalLocalPosition;
            capturedCamera.localRotation = capturedCameraOriginalLocalRotation;
        }

        GridEntity.OnPositionTransition += SaveAfterMove;
        player.InjectForcedMovement(releaseMovement);

        OnReleasePlayer?.Invoke(player);

        var manager = Dungeon.Enemies.FirstOrDefault(e => e.Identifier == "Manager");
        if (manager == null)
        {
            Debug.LogWarning("There's no manager in the office!");
        } else
        {
            var enemy = manager.GetComponent<TDEnemy>();
            if (enemy != null)
            {
                enemy.ForceActivity(LMCore.EntitySM.State.StateType.Patrolling);
                var patrolling = enemy.ActivePatrolling;
                if (patrolling != null)
                {
                    patrolling.ForceSetCheckpoint(0, 0, 1);
                }
            } else
            {
                Debug.LogError("The manager/enemy doesn't have an enemy script");
            }
        }

        if (AnomalyManager.instance.Weekday == Weekday.Monday || notShownPauseHintToday)
        {
            showingHint = true;
            SyncPauseSettingsHint();
        }

        player = null;
        releasing = false;
    }

    private void SaveAfterMove(GridEntity entity)
    {
        if (entity.EntityType != GridEntityType.PlayerCharacter) return;

        WWSaveSystem.SafeInstance.AutoSave();
        GridEntity.OnPositionTransition -= SaveAfterMove;
    }

    bool showingHint = false;

    void SyncPauseSettingsHint()
    {
        if (showingHint) {
            var keyHint = InputBindingsManager
                .InstanceOrResource("InputBindingsManager")
                .GetActiveCustomHint("Binding.ShowMenus");

            PromptUI.instance.ShowText($"{keyHint} Pause Game & Settings", showPauseHintDuration);
            notShownPauseHintToday = false;

            StartCoroutine(NoLiveUpdateHint());
        }
    }

    IEnumerator<WaitForSeconds> NoLiveUpdateHint()
    {
        yield return new WaitForSeconds(showPauseHintDuration);
        showingHint = false;
    }

    float releaseStart;
    bool releasing;

    [SerializeField]
    AnimationCurve verticalEase;

    [SerializeField]
    AnimationCurve remainingTranslationEase;

    [SerializeField]
    AnimationCurve rotationEase;

    [SerializeField]
    float releaseDuration = 0.7f;

    private void Update()
    {
        if (!releasing) return;

        float progress = Mathf.Clamp01((Time.timeSinceLevelLoad - releaseStart) / releaseDuration);

        Vector3 start = cameraPosition.position;
        Vector3 target = capturedCameraOriginalParent.TransformPoint(capturedCameraOriginalLocalPosition);
        Vector3 offset = target - start;
        Vector3 verticalOffset = new Vector3(0, offset.y);
        Vector3 remainingOffset = new Vector3(offset.x, 0f, offset.z);

        capturedCamera.position = start +
            Vector3.Lerp(Vector3.zero, verticalOffset, verticalEase.Evaluate(progress)) +
            Vector3.Lerp(Vector3.zero, remainingOffset, remainingTranslationEase.Evaluate(progress));

        capturedCamera.rotation = Quaternion.Lerp(cameraPosition.rotation, capturedCameraOriginalParent.rotation, progress);

        if (progress == 1f)
        {
            CompleteReleasePlayer();
        }
    }
}
