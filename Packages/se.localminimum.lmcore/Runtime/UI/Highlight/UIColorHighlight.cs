using LMCore.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LMCore.UI
{
    public class UIColorHighlight : MonoBehaviour
    {
        [HelpBox("If selectable is provided interactable status will follow from it.")]
        [SerializeField]
        Selectable source;

        [SerializeField]
        bool syncSelectedOnEnable;

        [SerializeField]
        MaskableGraphic target;

        [SerializeField]
        Color defaultColor;

        [SerializeField]
        Color selectedColor;

        [SerializeField]
        Color hoveredColor;

        [SerializeField]
        Color pressedColor;

        [SerializeField]
        Color disabledColor;

        HighlightState state = HighlightState.Default;

        public void Set(HighlightState flag)
        {
            state |= flag;
            Sync();
        }

        public void Remove(HighlightState flag)
        {
            state &= ~flag;
            Sync();
        }

        public void PointerEnter() => Set(HighlightState.Hovered);
        public void PointerExit() => Remove(HighlightState.Hovered);
        public void PointerDown() => Set(HighlightState.Pressed);
        public void PointerUp() => Remove(HighlightState.Pressed);
        public void Focus()
        {
            Set(HighlightState.Selected);
        }

        public void Blur()
        {
            Remove(HighlightState.Selected);
            if (source != null)
            {
                SyncInteractable();
            }
        }
        public void Interactable() => Remove(HighlightState.Disabled);
        public void NonInteractable() => Set(HighlightState.Disabled);

        private void OnEnable()
        {
            if (source != null)
            {
                SyncInteractable();
                if (syncSelectedOnEnable && source.gameObject == EventSystem.current.currentSelectedGameObject)
                {
                    Focus();
                }
            }
            Sync();
        }

        private void OnDisable()
        {
            // Clear everything but disabled if script is disabled
            state &= HighlightState.Disabled;
        }

        void SyncInteractable()
        {
            if (state.HasFlag(HighlightState.Disabled) == source.interactable)
            {
                if (source.interactable)
                {
                    Interactable();
                } else
                {
                    NonInteractable();
                }
            }
        }

        void Sync()
        {
            if (target == null) return;

            switch (state.PriorityFlag()) {

                case HighlightState.Default:
                    target.color = defaultColor;
                    break;

                case HighlightState.Selected:
                    target.color = selectedColor;
                    break;

                case HighlightState.Hovered:
                    target.color = hoveredColor;
                    break;

                case HighlightState.Pressed:
                    target.color = pressedColor;
                    break;

                case HighlightState.Disabled:
                    target.color = disabledColor;
                    break;

                default:
                    Debug.LogWarning($"ColorHighlight '{name}': Don't know color for {state.PriorityFlag()}");
                    break;
            }
        }

        private void Update()
        {
            if (source != null) SyncInteractable();
        }
    }
}
