using UnityEngine;

namespace Unity.LEGO.Behaviours.Actions
{
    public abstract class RepeatableAction : Action
    {
        [SerializeField, Tooltip("The pause in seconds between each movement.")]
        protected float m_Pause = 1.0f;

        [SerializeField, Tooltip("Repeat this Behaviour continuously.")]
        protected bool m_Repeat = true;

        protected virtual void OnValidate()
        {
            m_Pause = Mathf.Max(0.0f, m_Pause);
        }
    }
}
