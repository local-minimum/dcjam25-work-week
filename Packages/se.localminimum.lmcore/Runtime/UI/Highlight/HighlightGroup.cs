using System.Collections.Generic;
using UnityEngine;

namespace LMCore.UI
{
    public class HighlightGroup : AbsHighlightable
    {
        [SerializeField]
        List<AbsHighlightable> targets = new List<AbsHighlightable>();


        protected override void Sync()
        {
            foreach (var target in targets)
            {
                target.SetState(state);
            }
        }
    }
}
