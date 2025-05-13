using System.Collections.Generic;
using UnityEngine;

public class PosterAnomalies : AbsAnomaly
{
    [SerializeField]
    float retoggleDelay = 0.5f;

    [SerializeField]
    Material BaseMat;

    [SerializeField]
    Material NormalMat;

    [SerializeField]
    Material AnomalyMat;

    [SerializeField]
    float angleThreshold = 60f;

    bool anomalyActive = false;

    Renderer _rend;
    Renderer rend
    {
        get
        {
            if (_rend == null)
            {
                _rend = GetComponent<Renderer>();
            }
            return _rend;
        }
    }

    Camera _playerCam;
    public Camera playerCam
    {
        get
        {
            if (_playerCam == null)
            {
                _playerCam = Dungeon.Player.GetComponentInChildren<Camera>(true);
            }

            return _playerCam;
        }
    }

    private void Start()
    {
    }

    protected override void OnDisableExtra()
    {
    }

    protected override void OnEnableExtra()
    {
    }

    protected override void SetAnomalyState()
    {
        anomalyActive = true;
    }

    protected override void SetNormalState()
    {
        anomalyActive = false;
        SetNormal();
    }

    float nextToggle;
    bool showingAnomaly;

    private void Update()
    {
        if (!anomalyActive || Time.timeSinceLevelLoad < nextToggle) return;


        bool trigger = CalculateIfAnomalyAngle();

        if (trigger && !showingAnomaly)
        {
            SetAnomaly();
            nextToggle = Time.timeSinceLevelLoad + retoggleDelay;
        } else if (!trigger && showingAnomaly)
        {
            SetNormal();
            nextToggle = Time.timeSinceLevelLoad + retoggleDelay;
        }
    }

    bool CalculateIfAnomalyAngle()
    {

        var normIn = transform.right;
        normIn.y = 0;

        var lookDirection = playerCam.transform.forward;
        lookDirection.y = 0;

        var forwardRot = Quaternion.LookRotation(normIn, Vector3.up);
        var toMeRot = Quaternion.LookRotation(lookDirection, Vector3.up);

        var angle = Quaternion.Angle(forwardRot, toMeRot);

        bool trigger = angle > angleThreshold;

        // Debug.Log($"Evil Posters: {angle} > {angleThreshold} => {trigger} vs {showingAnomaly} (Forward: {normIn}, To me: {lookDirection})");

        return trigger;
    }

    [ContextMenu("Info")]
    void Info() => CalculateIfAnomalyAngle();

    void SetNormal()
    {
        rend.SetMaterials(new List<Material>() { BaseMat, NormalMat });
        showingAnomaly = false;
    }

    void SetAnomaly()
    {
        rend.SetMaterials(new List<Material>() { BaseMat, AnomalyMat });
        showingAnomaly = true;
    }
}
