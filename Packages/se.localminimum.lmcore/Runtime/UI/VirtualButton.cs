using UnityEngine;
using UnityEngine.Events;

namespace LMCore.UI
{
    [RequireComponent(typeof(Collider))]
    public class VirtualButton : MonoBehaviour
    {
        [SerializeField]
        UnityEvent OnClick;

        public void Click() => OnClick?.Invoke();
    }
}
