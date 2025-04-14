using UnityEngine;
using UnityEngine.Events;

namespace LMCore.UI
{
    public class VirtualButton : MonoBehaviour
    {
        [SerializeField]
        UnityEvent OnClick;

        public void Click() => OnClick?.Invoke();
    }
}
