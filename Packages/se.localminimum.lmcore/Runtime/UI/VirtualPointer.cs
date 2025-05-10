using LMCore.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LMCore.UI
{
    public class VirtualPointer : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField]
        Vector2 padding;

        [SerializeField]
        Camera cam;

        [SerializeField, Range(0, 30)]
        float gamepadInputScale = 5f;

        [SerializeField, Range(0, 10)]
        float speedFactor = 1f;

        [SerializeField]
        LayerMask uiLayer;

        [SerializeField]
        float maxDistance = 1000;

        [SerializeField, Range(0, 1)]
        float afterClickRespite = 0.4f;

        private void OnEnable()
        {
            Cursor.visible = false;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas.renderMode != RenderMode.ScreenSpaceCamera)
            {
                Debug.LogWarning("Virtual pointer only coded for screen space cameras");
            }
        }

        private void OnDisable()
        {
            Cursor.visible = true;
        }

        
        GameObject hovered;

        public void HandleCursorDelta(Vector2 delta)
        {
            var canvasRT = GetComponentInParent<Canvas>().transform as RectTransform;
            var halfWidth = canvasRT.rect.width / 2f;
            var halfHeight = canvasRT.rect.height / 2f;

            var mouseDelta = delta * speedFactor;
            var rt = transform as RectTransform;

            var anchor = rt.anchoredPosition;
            anchor += mouseDelta;
            anchor.x = Mathf.Clamp(anchor.x, -halfWidth + padding.x, halfWidth - padding.x);
            anchor.y = Mathf.Clamp(anchor.y, -halfHeight + padding.y, halfHeight - padding.y);

            bool moved = rt.anchoredPosition != anchor;
            rt.anchoredPosition = anchor;

            if (moved)
            {
                HandleHover();
            }
        }

        Vector2 lastGamepadDirection;

        public void HandleTranslate(InputAction.CallbackContext context)
        {
            lastGamepadDirection = context.ReadValue<Vector2>() * gamepadInputScale;
        }

        void Update()
        {
            if (Cursor.visible) { Cursor.visible = false; }

            switch (ActionMapToggler.LastDevice)
            {
                case Extensions.SimplifiedDevice.XBoxController:
                case Extensions.SimplifiedDevice.PSController:
                case Extensions.SimplifiedDevice.SwitchController:
                case Extensions.SimplifiedDevice.OtherController:
                    HandleCursorDelta(lastGamepadDirection);
                    break;
                case Extensions.SimplifiedDevice.MouseAndKeyboard:
                    HandleCursorDelta(Mouse.current.delta.value);
                    break;
            }
        }

        void HandleHover()
        {
            if (GetHit(out GameObject hovered))
            {
                if (this.hovered != hovered)
                {
                    RemoveHover(this.hovered);
                }

                var vbutton = hovered.GetComponent<VirtualButton>();
                if (vbutton != null)
                {
                    vbutton.PointerEnter();
                }
            } else if (this.hovered != null)
            {
                RemoveHover(this.hovered);
            }

            this.hovered = hovered;
        }

        void RemoveHover(GameObject go)
        {
            if (go != null)
            {
                var previousButton = go.GetComponent<VirtualButton>();
                if (previousButton != null)
                {
                    previousButton.PointerExit();
                }
            }
        }

        bool GetHit(out GameObject go)
        {
            var rt = transform as RectTransform;
            var screenPoint = RectTransformUtility.WorldToScreenPoint(cam, rt.position);
            var renderRay = cam.ScreenPointToRay(screenPoint);

            RaycastHit hit;
            if (Physics.Raycast(renderRay, out hit, maxDistance, uiLayer))
            {
                go = hit.collider.gameObject;
                return true;
            }

            go = null;
            return false;
        }

        float nextClickAllowedAt;

        public void Click()
        {
            if (!enabled) return;

            if (Time.realtimeSinceStartup > nextClickAllowedAt && GetHit(out var go))
            {
                nextClickAllowedAt = Time.realtimeSinceStartup + afterClickRespite;

                HandleHit(go);
            }
        }

        void HandleHit(GameObject go)
        {
            Debug.Log($"Hit: {go}");
            var btn = go.GetComponent<VirtualButton>();
            if (btn != null) btn.Click();
        }

        public void HandleClickCallback(InputAction.CallbackContext context)
        {
            if (context.performed) Click();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Click();
        }
    }
}
