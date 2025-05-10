using UnityEngine;
using UnityEngine.UI;

namespace LMCore.UI
{
    public class UIColorHighlight : AbsHighlightable 
    {
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

        override protected void Sync()
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
    }
}
