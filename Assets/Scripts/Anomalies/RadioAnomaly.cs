using LMCore.Crawler;
using LMCore.TiledDungeon;
using LMCore.TiledDungeon.DungeonFeatures;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RadioAnomaly : AbsAnomaly
{
    [SerializeField]
    AudioSource speaker;

    [SerializeField]
    AudioClip anomalyTrack;

    [SerializeField]
    float maxDistanceRollOffAnomaly = 10f;

    [SerializeField]
    AudioClip normalTrack;

    [SerializeField]
    float maxDistanceRollOffNormal = 5f;

    [SerializeField]
    List<Vector3Int> triggeringLocationOffsets = new List<Vector3Int>();

    List<TDNode> _triggerLocations;
    List<TDNode> triggerLocations
    {
        get
        {
            if (_triggerLocations == null)
            {
                _triggerLocations = new List<TDNode>();
                foreach (var offset in triggeringLocationOffsets)
                {
                    var coords = Coordinates + offset;
                    var node = Dungeon[coords];
                    if (node != null)
                    {
                        _triggerLocations.Add(node);
                    }
                }
            }
            return _triggerLocations;
        }
    }

    [SerializeField]
    Transform radio;

    public static void TrackPlayer(GridEntity player, Transform transform)
    {
        var looktarget = player.transform.position;
        looktarget.y = transform.position.y;
        transform.LookAt(looktarget);
    }


    [System.Serializable]
    public class Jitter
    {
        [SerializeField, Range(0, 1)]
        float jitterMax = 0.1f;

        [SerializeField, Range(0, 1)]
        float jitterFreq = 0.1f;

        float nextJitter;

        public void Apply(Transform radio)
        {
            if (radio != null && Time.timeSinceLevelLoad > nextJitter)
            {
                radio.transform.localPosition = new Vector3(Random.Range(-jitterMax, jitterMax), Random.value * jitterMax, Random.Range(-jitterMax, jitterMax));
                nextJitter = Time.timeSinceLevelLoad + jitterFreq;
            }
        }
    }

    [SerializeField]
    Jitter jitter = new Jitter();

    [SerializeField]
    TDDecoration managerSpawn;

    [SerializeField]
    TDDecoration managerTarget;

    bool anomalous;
    public bool Activated { get; private set; }

    void PlayNormalTrack() => PlayTrack(speaker, normalTrack, maxDistanceRollOffNormal);
    void PlayAnomalyTrack() => PlayTrack(speaker, anomalyTrack, maxDistanceRollOffAnomaly);

    public static void PlayTrack(AudioSource speaker, AudioClip track, float maxDistanceRollOff)
    {
        if (speaker != null)
        {
            if (track == null)
            {
                speaker.Stop();
            } else
            {
                speaker.clip = track;
                speaker.Play();
            }
            speaker.maxDistance = maxDistanceRollOff;
        }
    }

    protected override void SetAnomalyState()
    {
        Debug.Log("Anomaly radio ready");
        anomalous = true;
        Activated = false;
        PlayNormalTrack();
    }

    protected override void SetNormalState()
    {
        Debug.Log("Anomaly radio not active");
        anomalous = false;
        PlayNormalTrack();
    }

    override protected void OnEnableExtra()
    {
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition; 
    }

    override protected void OnDisableExtra()
    {
        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition; 
    }

    GridEntity player;

    private void GridEntity_OnPositionTransition(GridEntity entity)
    {
        if (!anomalous || Activated || entity.EntityType != GridEntityType.PlayerCharacter) return;

        player = entity;
        if (triggerLocations != null && triggerLocations.Any(n => n.Coordinates == entity.Coordinates))
        {
            Activated = true;
            Debug.Log($"Radio anomaly activated at {entity.Coordinates}");
        }
        if (Activated) Activation();
    }

    void Activation()
    {
        PlayAnomalyTrack();

        if (WWSettings.ManagerPersonality.Value != ManagerPersonality.Golfer)
        {
            var manager = Dungeon.GetEntity("Manager", includeDisabled: true);
            if (manager != null)
            {
                var controller = manager.GetComponent<ManagerPersonalityController>();
                if (controller != null)
                {
                    controller.RestoreEnemyAt(
                        managerSpawn.GetComponentInParent<TDNode>(),
                        Direction.West,
                        managerTarget.GetComponentInParent<TDPathCheckpoint>());
                }
            }
        }
    }


    private void Update()
    {
        if (!Activated) return;

        TrackPlayer(player, transform);

        if (jitter != null)
        {
            jitter.Apply(radio);
        }
    }
}
