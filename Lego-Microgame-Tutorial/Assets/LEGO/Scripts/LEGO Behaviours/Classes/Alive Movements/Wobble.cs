using UnityEngine;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.Behaviours
{
    public class Wobble : AliveMovement
    {
        Vector3 m_Axis;
        float m_Extents;

        public Wobble() : base(0.8f)
        {
        }

        public override void Initialise(Bounds bounds, AliveAction.Type type)
        {
            base.Initialise(bounds, type);

            if (Random.value > 0.5f)
            {
                m_Axis = Vector3.forward * (Random.value > 0.5f ? 1.0f : -1.0f);
                m_Extents = m_Bounds.extents.x;
            }
            else
            {
                m_Axis = Vector3.right * (Random.value > 0.5f ? 1.0f : -1.0f);
                m_Extents = m_Bounds.extents.z;
            }

            switch (m_Type)
            {
                case AliveAction.Type.Creature:
                    {
                        m_TypeScale = 1.0f;
                        break;
                    }
                case AliveAction.Type.Robot:
                    {
                        m_TypeScale = 1.3f;
                        break;
                    }
            }
        }

        public override void UpdateMovement(float time)
        {
            var clampedTime = Mathf.Min(1.0f, time / Time);
            var rotationAngle = Mathf.Clamp(Mathf.Sin(clampedTime * 2.0f * Mathf.PI) * m_TypeScale, -1.0f, 1.0f) * 10.0f;
            m_Rotation = m_Axis * rotationAngle;
            m_Offset = Vector3.up * Mathf.Abs(Mathf.Tan(rotationAngle * Mathf.Deg2Rad) * m_Extents);
        }
    }
}