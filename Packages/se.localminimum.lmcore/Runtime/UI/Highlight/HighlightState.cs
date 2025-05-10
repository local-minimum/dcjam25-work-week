using System;

namespace LMCore.UI
{
    [Flags]
    public enum HighlightState
    {
        Default = 0,
        Selected = 1,
        Hovered = 2,
        Pressed = 4,
        Disabled = 8
    }

    public static class HighlightStateExtentsions
    {
        public static HighlightState PriorityFlag(this HighlightState state)
        {
            if (state.HasFlag(HighlightState.Disabled)) return HighlightState.Disabled;
            if (state.HasFlag(HighlightState.Pressed)) return HighlightState.Pressed;
            if (state.HasFlag(HighlightState.Hovered)) return HighlightState.Hovered;
            if (state.HasFlag(HighlightState.Selected)) return HighlightState.Selected;
            return HighlightState.Default;
        }
    }
}
