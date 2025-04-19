using LMCore.Crawler;
using LMCore.Extensions;
using System.Collections.Generic;
using UnityEngine;

public class GridEntityFootSounds : MonoBehaviour
{
    [SerializeField]
    AudioSource speaker;

    [SerializeField]
    List<AudioClip> clips = new List<AudioClip>();

    [SerializeField]
    float volumeScale = 0.5f;

    GridEntity myEntity;

    private void OnEnable()
    {
        myEntity = GetComponentInParent<GridEntity>(true);
        GridEntity.OnMove += GridEntity_OnMove;
    }

    private void GridEntity_OnMove(GridEntity entity)
    {
        if (entity != myEntity || entity.Moving != MovementType.Translating) return;

        speaker.PlayOneShot(clips.GetRandomElement(), volumeScale);
    }
}
