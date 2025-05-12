using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.TiledDungeon;
using LMCore.TiledDungeon.DungeonFeatures;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AttackingBoxesAnomaly : AbsAnomaly
{
    [SerializeField]
    List<GameObject> boxes = new List<GameObject>();

    [SerializeField]
    float deltaTime = 0.4f;

    [SerializeField]
    List<Transform> intermediatCheckpoints = new List<Transform>();

    [SerializeField]
    float throwPower = 400f;

    [SerializeField]
    AudioSource speaker;

    [SerializeField]
    TDDecoration managerSpawn;

    [SerializeField]
    TDDecoration managerTarget;

    protected override void OnEnableExtra()
    {
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
        if (speaker != null) speaker.Stop();
    }

    protected override void OnDisableExtra()
    {
        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
        if (speaker != null) speaker.Stop();
    }


    bool triggered = false;
    GridEntity player;
    bool completed = false;
    bool activeAnomaly;
    float startTime;


    private void GridEntity_OnPositionTransition(GridEntity entity)
    {

        if (!activeAnomaly || triggered || completed) return;

        // Debug.Log($"BadBox: Coordinates {Coordinates} match {entity.Coordinates == Coordinates} and {entity.EntityType}==Player {entity.EntityType == GridEntityType.PlayerCharacter}");

        if (entity.EntityType == GridEntityType.PlayerCharacter && entity.Coordinates == Coordinates)
        {
            boxes = boxes.Shuffle().ToList();
            triggered = true;
            player = entity;
            startTime = Time.timeSinceLevelLoad;
            Debug.Log($"BadBox: Ready to throw {boxes.Count} boxes");
            if (speaker != null)
            {
                speaker.Play();
            }

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
    }

    protected override void SetAnomalyState()
    {
        activeAnomaly = true;
    }

    protected override void SetNormalState()
    {
        activeAnomaly = false;
        if (speaker != null) speaker.Stop();
    }

    List<GameObject> thrownBoxes = new List<GameObject>();
    Dictionary<GameObject, Transform> midpoints = new Dictionary<GameObject, Transform>();
    Dictionary<GameObject, Vector3> boxStarts = new Dictionary<GameObject, Vector3>();

    private void Update()
    {
        if (completed || !triggered || !activeAnomaly || player == null) return;

        float t = Time.timeSinceLevelLoad - startTime;
        var lastBox = Mathf.Min(boxes.Count - 1, Mathf.FloorToInt(t / deltaTime));
        for (int i = 0; i<=lastBox; i++)
        {
            var box = boxes[i];
            if (thrownBoxes.Contains(box)) continue;

            Transform target = null;
            if (!midpoints.ContainsKey(box))
            {
                Debug.Log($"BadBox: Starts lerping {box}");
                target = intermediatCheckpoints
                    .OrderBy(pt => (box.transform.position - pt.position).sqrMagnitude)
                    .FirstOrDefault();

                if (target == null) break;

                midpoints.Add(box, target);
                boxStarts.Add(box, box.transform.position);
            } else
            {
                target = midpoints[box];
            }

            if (target == null) continue;

            var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - (startTime + deltaTime * i)));

            box.transform.position = Vector3.Lerp(boxStarts[box], target.position, progress);
            if (progress == 1f)
            {
                Debug.Log($"BadBox: throws {box}");
                thrownBoxes.Add(box);
                foreach (var mr in box.GetComponentsInChildren<MeshRenderer>())
                {
                    mr.gameObject.AddComponent<BoxCollider>();
                }
                var rb = box.AddComponent<Rigidbody>();
                var throwVector = (player.LookTarget.position - box.transform.position);
                rb.AddForce(throwVector * throwPower, ForceMode.Impulse);

                if (i == boxes.Count - 1)
                {
                    completed = true;
                }
            }
        }
    }
}
