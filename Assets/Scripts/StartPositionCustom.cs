using LMCore.Crawler;
using LMCore.IO;
using LMCore.TiledDungeon;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledImporter;
using UnityEngine;

public delegate void StartPositionPlayerEvent(GridEntity player);

public class StartPositionCustom : TDFeature, ITDCustom
{
    public static event StartPositionPlayerEvent OnReleasePlayer;
    public static event StartPositionPlayerEvent OnCapturePlayer;

    [SerializeField]
    Transform cameraPosition;

    [SerializeField]
    Movement releaseMovement = Movement.Backward;

    public void Configure(TDNode node, TiledCustomProperties properties)
    {
    }

    private void OnEnable()
    {
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
        ActiveOS.OnReleasePlayer += ReleasePlayer;
    }

    private void OnDisable()
    {
        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
        ActiveOS.OnReleasePlayer -= ReleasePlayer;
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

        OnCapturePlayer?.Invoke(player);
    }

    [ContextMenu("Release player")]
    void ReleasePlayer()
    {
        releasing = true;
        releaseStart = Time.timeSinceLevelLoad;
    }

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

        player.InjectForcedMovement(releaseMovement);

        OnReleasePlayer?.Invoke(player);

        player = null;
        releasing = false;
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
