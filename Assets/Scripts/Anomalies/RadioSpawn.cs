using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.TiledDungeon.DungeonFeatures;
using UnityEngine;

public class RadioSpawn : TDFeature 
{
    [SerializeField]
    RadioAnomaly anomaly;

    [SerializeField, Range(1, 10)]
    int spawnDistance = 5;

    [SerializeField]
    Transform radio;

    [SerializeField]
    AudioSource speaker;

    [SerializeField]
    AudioClip anomalyTrack;

    [SerializeField]
    float maxDistanceRollOffAnomaly = 10f;

    [SerializeField]
    RadioAnomaly.Jitter jitter = new RadioAnomaly.Jitter();

    private void OnEnable()
    {
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition; 
    }

    private void OnDisable()
    {
        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition; 
    }

    GridEntity player;
    private void GridEntity_OnPositionTransition(GridEntity entity)
    {
        if (activated || anomaly == null || !anomaly.Activated || entity.EntityType != GridEntityType.PlayerCharacter) return;

        player = entity;
        activated = entity.Coordinates.ManhattanDistance(Coordinates) <= spawnDistance;

        if (activated)
        {
            Debug.Log($"Radio Spawn activated at {Coordinates}");
            radio.gameObject.SetActive(true);
            RadioAnomaly.PlayTrack(speaker, anomalyTrack, maxDistanceRollOffAnomaly);
        }
    }

    private void Start()
    {
        radio.gameObject.SetActive(false);
    }

    bool activated;

    private void Update()
    {
        if (!activated) return;

        RadioAnomaly.TrackPlayer(player, transform);

        if (jitter != null) jitter.Apply(radio);
    }
}
