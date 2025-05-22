using LMCore.Extensions;
using LMCore.IO;
using LMCore.UI;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LMCore.Crawler
{
    public delegate void FreeLookCameraEvent();

    public class FreeLookCamera : MonoBehaviour
    {
        [Flags]
        public enum SnapbackMode
        {
            None,
            ByActivationToggle,
            ByActivationRelease,
            ByManualReset,
            ByMovement
        };

        public event FreeLookCameraEvent OnFreeLookCameraEnable;
        public event FreeLookCameraEvent OnFreeLookCameraDisable;

        [HelpBox("This script should be on a parent to the camera itself to work properly", HelpBoxMessageType.Warning)]
        [SerializeField, Header("Snapback")]
        protected SnapbackMode MouseSnapBack = SnapbackMode.ByActivationRelease;
        [SerializeField]
        protected SnapbackMode ControllerSnapBack = SnapbackMode.ByActivationToggle;

        protected SnapbackMode SnapBack => controllerMode ? ControllerSnapBack : MouseSnapBack;

        [SerializeField, Range(0, 1), Tooltip("0 = No snapback, 1 = Instant")]
        float snapbackLerp = 0.2f;
        [SerializeField, Tooltip("If angle from resting rotation is less than this, stop lerping / reset")]
        float identityThreshold = 1f;

        [SerializeField]
        float controllerNoLookAroundTime = 0.5f;

        [SerializeField, Header("Freedom cone"), Range(0, 1)]
        float lookEasingSpeed = 0.01f;

        [SerializeField, Range(0, 90f)]
        float verticalLookMaxAngle = 40f;

        [SerializeField, Range(0, 90f)]
        float horizontalLookMaxAngle = 60f;

        [SerializeField, Range(0f, 3f)]
        float forwardTranslation = 1f;

        [SerializeField]
        bool manageNativeCursor = true;

        bool overriddenForwardTranslation;
        float translationForwardOverride;

        float ForwardTranslation => 
            overriddenForwardTranslation ? translationForwardOverride : forwardTranslation;

        [SerializeField, Range(0, 1)]
        float translationLerp = 0.05f;

        [SerializeField, Range(0, 1)]
        float controllerSensitivity = 0.05f;

        [SerializeField, Range(0, 1)]
        float mouseSensitivity = 0.05f;

        float _controllerLookAroundTime;
        bool _freeLooking;
        bool freeLooking
        {
            get => _freeLooking;

            set
            {
                _freeLooking = value;
                _controllerLookAroundTime = Time.timeSinceLevelLoad + controllerNoLookAroundTime;
                if (value)
                {
                    virtualPointerCoords = Vector2.zero;
                    OnFreeLookCameraEnable?.Invoke();
                } else
                {
                    OnFreeLookCameraDisable?.Invoke();
                }

                Debug.Log($"Looking {freeLooking} at {virtualPointerCoords}");
            }
        }

        GridEntity _entity;
        GridEntity Entity
        {
            get
            {
                if (_entity == null)
                {
                    _entity = GetComponentInParent<GridEntity>();
                }
                return _entity;
            }
        }

        CustomCursor _cusomCursor;
        CustomCursor customCursor
        {
            get
            {
                if (_cusomCursor == null)
                {
                    _cusomCursor = GetComponentInParent<CustomCursor>(true);
                }

                return _cusomCursor;
            }
        }

        public void SetTranslationForwardOverride(float forward = 0)
        {
            translationForwardOverride = forward;
            overriddenForwardTranslation = true;
        }

        public void RemoveTranslationForwardOverride()
        {
            overriddenForwardTranslation = false;
        }

        bool NativeCursorAllowed =>
            customCursor == null ? true : customCursor.NativeCursorShouldBeVisible;

        void SyncNativeCursor()
        {
            if (manageNativeCursor)
            {
                Cursor.visible = !freeLooking && NativeCursorAllowed;
            }
        }

        public Camera cam { get; private set; }

        bool allowed = true;

        Vector2 restorePoint;
        void RememberCursorPosition()
        {
            restorePoint = Mouse.current.position.ReadValue();
        }

        void RestoreCursorPosition()
        {
            Mouse.current.WarpCursorPosition(restorePoint);
        }

        public void OnFreeLook(InputAction.CallbackContext context)
        {
            if (!enabled) return;

            bool allowNative = NativeCursorAllowed;

            if (SnapBack.HasFlag(SnapbackMode.ByActivationToggle))
            {
                if (context.performed)
                {
                    freeLooking = !freeLooking;
                    if (freeLooking)
                    {
                        RememberCursorPosition();
                    }
                    else
                    {
                        RestoreCursorPosition();
                    }
                    SyncNativeCursor();
                }
            }
            else if (SnapBack.HasFlag(SnapbackMode.ByActivationRelease))
            {
                if (context.performed)
                {
                    freeLooking = true;
                    RememberCursorPosition();
                    SyncNativeCursor();
                }
                else if (context.canceled)
                {
                    freeLooking = false;
                    RestoreCursorPosition();
                    SyncNativeCursor();
                }
            }
            else if (context.performed)
            {
                freeLooking = true;
                RememberCursorPosition();
                SyncNativeCursor();
            }
        }

        public void RefuseFreelook()
        {
            allowed = false;

            if (!enabled) return;
            SyncNativeCursor();
        }

        public void AllowFreelook()
        {
            allowed = true;

            if (!enabled) return;
            SyncNativeCursor();
        }

        Vector2 virtualPointerCoords;
        public void OnPointer(InputAction.CallbackContext context)
        {
            if (!allowed || !enabled || !freeLooking) return;
            if (controllerMode && Time.timeSinceLevelLoad < _controllerLookAroundTime) return;

            var centerOffset = context.ReadValue<Vector2>();

            virtualPointerCoords += centerOffset * (controllerMode ? controllerSensitivity : mouseSensitivity);

            virtualPointerCoords = virtualPointerCoords.ClampDimensions(-1, 1);
        }

        private void Start()
        {
            cam = GetComponentInChildren<Camera>(true);
        }

        private void OnEnable()
        {
            GridEntity.OnMove += GridEntity_OnMove;
            GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
            ActionMapToggler.OnChangeControls += ActionMapToggler_OnChangeControls;
        }

        private void OnDisable()
        {
            GridEntity.OnMove -= GridEntity_OnMove;
            GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
            ActionMapToggler.OnChangeControls -= ActionMapToggler_OnChangeControls;
        }

        bool controllerMode;
        private void ActionMapToggler_OnChangeControls(PlayerInput input, string controlScheme, SimplifiedDevice device)
        {
            virtualPointerCoords = Vector2.zero;
            controllerMode = device.IsController();
        }

        private void GridEntity_OnMove(GridEntity entity)
        {
            if (!enabled) return;

            if (Entity == entity && SnapBack.HasFlag(SnapbackMode.ByMovement))
            {
                if (freeLooking) RestoreCursorPosition();
                freeLooking = false;
                SyncNativeCursor();
            }
        }

        private void GridEntity_OnPositionTransition(GridEntity entity)
        {
            if (entity != Entity) return;

            var anchor = entity.NodeAnchor;
            if (anchor == null)
            {
                AllowFreelook();
            }
            else
            {
                var constraint = anchor.Constraint;
                if (constraint == null || !constraint.RefuseFreeCamera)
                {
                    AllowFreelook();
                }
                else
                {
                    if (freeLooking) RestoreCursorPosition();
                    RefuseFreelook();
                }
            }
        }

        private void Update()
        {
            var entity = Entity;
            if (allowed && freeLooking)
            {
                var euler = transform.eulerAngles;
                var target = Quaternion.Euler(euler.x + (virtualPointerCoords.y) * -verticalLookMaxAngle, euler.y + (virtualPointerCoords.x) * horizontalLookMaxAngle, euler.z);
                cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation, target, lookEasingSpeed);

                // Only translate forward when not on wall
                var z = cam.transform.localPosition.z;
                if (!Entity.AnchorDirection.IsPlanarCardinal())
                {
                    cam.transform.localPosition = Vector3.forward * Mathf.Lerp(z, ForwardTranslation, translationLerp);
                }
                else
                {
                    cam.transform.localPosition = Vector3.forward * Mathf.Lerp(z, 0, 0.5f);
                }
            }
            else if (cam.transform.localRotation != Quaternion.identity)
            {
                cam.transform.localRotation = Quaternion
                    .Lerp(cam.transform.localRotation, Quaternion.identity, snapbackLerp);

                var z = cam.transform.localPosition.z;
                cam.transform.localPosition = Vector3.forward * Mathf.Lerp(z, 0, 0.5f);

                if (Quaternion.Angle(cam.transform.localRotation, Quaternion.identity) < identityThreshold)
                {
                    cam.transform.localRotation = Quaternion.identity;
                    cam.transform.localPosition = Vector3.zero;
                }
            }
        }
    }
}
