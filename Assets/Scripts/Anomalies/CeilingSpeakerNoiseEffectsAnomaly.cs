using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.TiledDungeon;
using System.Collections.Generic;
using UnityEngine;

public class CeilingSpeakerNoiseEffectsAnomaly : AbsAnomaly
{
    [SerializeField]
    Transform speakersModel;

    [SerializeField]
    float pitchThreshold = -25;

    [SerializeField]
    float maxAngle = 20f;

    [SerializeField]
    AudioSource speakers;

    [SerializeField]
    List<AudioClip> sounds = new List<AudioClip>();

    protected override void OnDisableExtra()
    {
        if (freeCam == null) return;

        freeCam.OnFreeLookCameraEnable -= FreeCam_OnFreeLookCameraEnable;
        freeCam.OnFreeLookCameraDisable -= FreeCam_OnFreeLookCameraDisable;
    }

    protected override void OnEnableExtra()
    {
        if (freeCam == null) return;

        freeCam.OnFreeLookCameraEnable += FreeCam_OnFreeLookCameraEnable;
        freeCam.OnFreeLookCameraDisable += FreeCam_OnFreeLookCameraDisable;
    }

    FreeLookCamera freeCam;
    protected override void SetAnomalyState()
    {
        freeCam = Dungeon.Player.GetComponent<TDPlayerEntity>().FreeLookCamera;
        if (freeCam == null)
        {
            Debug.LogError($"Free Cam doesn't exist of player {Dungeon.Player.name}");
            return;
        }

        freeCam.OnFreeLookCameraEnable += FreeCam_OnFreeLookCameraEnable;
        freeCam.OnFreeLookCameraDisable += FreeCam_OnFreeLookCameraDisable;
    }

    protected override void SetNormalState()
    {
        if (freeCam == null) return;

        freeCam.OnFreeLookCameraEnable -= FreeCam_OnFreeLookCameraEnable;
        freeCam.OnFreeLookCameraDisable -= FreeCam_OnFreeLookCameraDisable;

        freeCam = null;
    }

    bool canTrigger;

    private void FreeCam_OnFreeLookCameraDisable()
    {
        canTrigger = false;
    }

    private void FreeCam_OnFreeLookCameraEnable()
    {
        canTrigger = true;
    }

    private void Update()
    {
        if (!canTrigger || speakers.isPlaying) return;

        var camTransform = freeCam.cam.transform;
        var pitch = camTransform.rotation.GetPitch() * Mathf.Rad2Deg;

        if (pitch > pitchThreshold) return;

        var toMeVector = (speakersModel.position - camTransform.position);
        var angle = Vector3.Angle(toMeVector, camTransform.forward);

        if (angle > maxAngle) return;

        speakers.PlayOneShot(sounds.GetRandomElement());
    }
}
