using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LMCore.UI
{
    [RequireComponent(typeof(EventTrigger))]
    [RequireComponent(typeof(RectTransform))]
    public class UICard : Selectable, IPointerClickHandler 
    {
        [SerializeField]
        UnityEvent OnClick;

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke();
        }
    }
}
