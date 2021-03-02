using UnityEngine;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.Behaviours
{
    public abstract class AliveMovement
    {
        public float Time { get; private set; }

        protected Bounds m_Bounds;

        protected float m_TypeScale;
        protected float m_TypeClamp;
        protected AliveAction.Type m_Type;

        protected Vector3 m_Offset = Vector3.zero;
        protected Vector3 m_Scale = Vector3.one;
        protected Vector3 m_Rotation = Vector3.zero;

        public AliveMovement(float time)
        {
            Time = time;
        }

        public virtual void Initialise(Bounds bounds, AliveAction.Type type)
        {
            m_Bounds = bounds;
            m_Type = type;
        }

        public abstract void UpdateMovement(float time);

        public Vector3 GetOffset()
        {
            return m_Offset;
        }

        public Vector3 GetScale()
        {
            return m_Scale;
        }

        public Vector3 GetRotation()
        {
            return m_Rotation;
        }
    }
}


