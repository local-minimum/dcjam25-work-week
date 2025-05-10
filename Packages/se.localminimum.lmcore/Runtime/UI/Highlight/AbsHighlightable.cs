using LMCore.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LMCore.UI
{
    public abstract class AbsHighlightable: MonoBehaviour
    {
        [HelpBox("If selectable is provided interactable status will follow from it.")]
        [SerializeField]
        Selectable source;

        [SerializeField]
        bool syncSelectedOnEnable;

        protected HighlightState state = HighlightState.Default;

        public void SetState(HighlightState state)
        {
            this.state = state;
            Sync();
        }

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

        abstract protected void Sync();

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

        private void Update()
        {
            if (source != null) SyncInteractable();
        }
    }
}
