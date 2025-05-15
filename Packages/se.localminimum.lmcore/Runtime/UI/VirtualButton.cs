using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LMCore.UI
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Collider))]
    public class VirtualButton : MonoBehaviour
    {
        [SerializeField]
        UnityEvent OnClick;

        [SerializeField]
        List<MaskableGraphic> effectsTargets = new List<MaskableGraphic>();

        [SerializeField]
        Color defaultColor;

        [SerializeField]
        Color hoverColor;

        [SerializeField]
        Color disabledColor;

        [SerializeField]
        bool disableGameObjectNonInteractable = true;

        public void Click()
        {
            if (Interactable && enabled)
            {
                OnClick?.Invoke();
            }
        }

        bool hovered;

        public void PointerEnter()
        {
            hovered = true;
            SyncColor();
        }

        public void PointerExit()
        {
            hovered = false;
            SyncColor();
        }

        Collider _raycastTarget;
        Collider RaycastTarget 
        {
            get
            {
                if (_raycastTarget == null)
                {
                    _raycastTarget = GetComponent<Collider>();
                }
                return _raycastTarget;
            }
        }

        bool _interactable = true;
        public bool Interactable { 
            get => _interactable;
            set
            {
                _interactable = value;

                RaycastTarget.enabled = value;

                SyncColor();

                if (disableGameObjectNonInteractable)
                {
                    gameObject.SetActive(value);
                }
            }
        }

        void SyncColor()
        {
            if (effectsTargets == null) return;

            var color = defaultColor;
            if (!Interactable)
            {
                color = disabledColor;
            } else if (hovered)
            {
                color = hoverColor;
            }

            for (int i = 0, l = effectsTargets.Count; i < l; i++)
            {
                var target = effectsTargets[i];

                if (target == null) continue;

                target.color = color;
            }
        }
    }
}
