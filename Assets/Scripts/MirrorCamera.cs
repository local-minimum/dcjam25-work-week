using LMCore.Crawler;
using Unity.VisualScripting;
using UnityEngine;

public class MirrorCamera : MonoBehaviour
{
    [SerializeField]
    Transform cameraTransform;

    [SerializeField]
    Renderer mirror;

    private void OnEnable()
    {
        GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
    }

    private void OnDisable()
    {
        GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
    }

    Camera entityCamera;
    MirrorCameraSentinel sentinel;

    private void GridEntity_OnPositionTransition(GridEntity entity)
    {
        if (entity.EntityType != GridEntityType.PlayerCharacter) return;

        if (sentinel == null)
        {
            entityCamera = entity.GetComponentInChildren<Camera>();
            sentinel = mirror.GetComponent<MirrorCameraSentinel>();
        }

        if (entityCamera == null || sentinel == null)
        {
            Debug.LogWarning($"Unexpected state, mirror sentinel {sentinel}, camera {entityCamera}");
            return;
        }

    }

    /*
    void LateUpdate()
    {
        if (sentinel != null && sentinel.VisibleToPlayer && null != mirror && null != entityCamera)
        {
            Vector3 entityCameraPosition = entityCamera.transform.position;
            Vector3 positionInMirrorSpace =
                mirror.transform.InverseTransformPoint(entityCameraPosition);

            positionInMirrorSpace.z = -positionInMirrorSpace.z;

            transform.position =
                mirror.transform.TransformPoint(
                    positionInMirrorSpace);
        }
    }
    */

    [SerializeField]
    Transform minPosition;

    [SerializeField]
    Transform maxPosition;
    private Vector3 ClosestPoint(Vector3 limit1, Vector3 limit2, Vector3 point)
    {
        Vector3 lineVector = limit2 - limit1;

        float lineVectorSqrMag = lineVector.sqrMagnitude;

        // Trivial case where limit1 == limit2
        if (lineVectorSqrMag < 1e-3f)
            return limit1;

        float dotProduct = Vector3.Dot(lineVector, limit1 - point);

        float t = -dotProduct / lineVectorSqrMag;

        return limit1 + Mathf.Clamp01(t) * lineVector;
    }

    private void LateUpdate()
    {
        if (sentinel != null && sentinel.VisibleToPlayer && null != mirror && null != entityCamera)
        {
            //Invert y rotation
            var rotation = 180 - entityCamera.transform.eulerAngles.y;
            var myEulers = transform.eulerAngles;
            myEulers.y = rotation;
            transform.eulerAngles = myEulers;


            //Approximate view angle
            var mirrorOnSlide = ClosestPoint(minPosition.position, maxPosition.position, mirror.transform.position);

            var camPt = entityCamera.transform.position;
            var lowPt = ClosestPoint(minPosition.position, mirrorOnSlide, camPt);
            var highPt = ClosestPoint(maxPosition.position, mirrorOnSlide, camPt);

            if (lowPt == highPt)
            {
                transform.position = lowPt;
            } else if (lowPt == mirrorOnSlide)
            {
                var d = (highPt - mirrorOnSlide).magnitude;
                var m = (mirrorOnSlide - maxPosition.position).magnitude;
                transform.position = Vector3.Lerp(mirrorOnSlide, maxPosition.position, d / m);
            } else
            {
                var d = (lowPt - mirrorOnSlide).magnitude;
                var m = (mirrorOnSlide - minPosition.position).magnitude;
                transform.position = Vector3.Lerp(mirrorOnSlide, minPosition.position, d / m);
            }
        }
    }



    private void OnValidate()
    {
        if (mirror != null)
        {
            var sentinel = mirror.GetComponent<MirrorCameraSentinel>();
            if (sentinel == null)
            {
                mirror.AddComponent<MirrorCameraSentinel>();
            }
        }
    }
}
