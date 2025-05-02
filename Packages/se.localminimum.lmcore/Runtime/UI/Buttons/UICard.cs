using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LMCore.UI
{
    [RequireComponent(typeof(EventTrigger))]
    [RequireComponent(typeof(RectTransform))]
    public class UICard : MonoBehaviour
    {
        [Flags]
        enum State { 
            Default = 0, 
            Disabled = 1, 
            Hovered = 2,
            Pressed = 4
        }

        State state = State.Default;

        [SerializeField]
        UnityEvent OnClick;

        [SerializeField]
        Color disabledColor;

        [SerializeField]
        Color defaultColor;

        [SerializeField]
        Color pressColor;

        [SerializeField]
        Color hoverColor;

        [SerializeField]
        List<MaskableGraphic> targets = new List<MaskableGraphic>();

        public bool Disabled
        {
            get => state.HasFlag(State.Disabled);
            set
            {
                if (value)
                {
                    state |= State.Disabled;
                } else
                {
                    state &= ~State.Disabled;
                }
                SyncColor();
            }
        }

        Color color
        {
            get
            {
                if (state.HasFlag(State.Disabled)) return disabledColor;
                if (state.HasFlag(State.Pressed)) return pressColor;
                if (state.HasFlag(State.Hovered)) return hoverColor;
                return defaultColor;
            }
        }

        void SyncColor()
        {
            var c = color;

            for (int i=0, l=targets.Count; i<l; i++)
            {
                targets[i].color = c;
            }
        }

        public void OnPointerEnter()
        {
            state |= State.Hovered;
            SyncColor();
        }

        public void OnPointerPress()
        {
            state |= State.Pressed;
            SyncColor();
        }

        public void OnPointerClick()
        {
            OnClick?.Invoke();
        }

        public void OnPointerRelease()
        {
            state &= ~State.Pressed;
            SyncColor();
        }

        public void OnPointerExit()
        {
            state &= ~State.Hovered;
            SyncColor();
        }

        private void Start()
        {
            SyncColor();
        }
    }
}
