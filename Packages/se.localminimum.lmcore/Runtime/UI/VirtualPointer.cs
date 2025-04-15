using LMCore.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LMCore.UI
{
    public class VirtualPointer : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField]
        Vector2 padding;

        [SerializeField]
        Camera cam;

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

        void Update()
        {
            if (Cursor.visible) { Cursor.visible = false; }

            var canvasRT = GetComponentInParent<Canvas>().transform as RectTransform;
            var halfWidth = canvasRT.rect.width / 2f;
            var halfHeight = canvasRT.rect.height / 2f;
            var mouseDelta = Mouse.current.delta.value * speedFactor;
            var rt = transform as RectTransform;

            var anchor = rt.anchoredPosition;
            anchor += mouseDelta;
            anchor.x = Mathf.Clamp(anchor.x, -halfWidth + padding.x, halfWidth - padding.x);
            anchor.y = Mathf.Clamp(anchor.y, -halfHeight + padding.y, halfHeight - padding.y);

            rt.anchoredPosition = anchor;
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
            if (Time.realtimeSinceStartup > nextClickAllowedAt && GetHit(out var go))
            {
                HandleHit(go);
                nextClickAllowedAt = Time.realtimeSinceStartup + afterClickRespite;
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
