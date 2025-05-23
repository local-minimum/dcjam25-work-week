using UnityEngine;

namespace LMCore.EntitySM.State.Critera
{
    public abstract class AbsCustomPassingCriteria : MonoBehaviour
    {
        [SerializeField, Tooltip("Disables itself after use")]
        protected bool OneTime = false;

        public abstract bool Passing { get; }
    }

    /// <summary>
    /// A criteria that is goverened by the state of a behaviour elsewhere
    /// </summary>
    [System.Serializable]
    public class CustomCriteria : ITransitionCriteria
    {
        [SerializeField]
        float Weight = 1f;

        [SerializeField]
        AbsCustomPassingCriteria criteria;

        [SerializeField, Tooltip("Make passing failing and failing passing")]
        bool Inverted = false;

        public bool Enabled => criteria == null ? false : criteria.enabled;

        public bool Permissable(Personality personality, out float weight)
        {
            weight = Weight;
            if (criteria == null)
            {
                return false;
            }

            if (Inverted)
            {
                return !criteria.Passing;
            }
            return criteria.Passing;
        }
    }
}
