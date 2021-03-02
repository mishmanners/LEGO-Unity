using UnityEngine;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.Behaviours
{
    public class Jump : AliveMovement
    {
        public Jump() : base(0.8f)
        {
        }

        public override void Initialise(Bounds bounds, AliveAction.Type type)
        {
            base.Initialise(bounds, type);

            switch (m_Type)
            {
                case AliveAction.Type.Creature:
                    {
                        m_TypeScale = 1.0f;
                        break;
                    }
                case AliveAction.Type.Robot:
                    {
                        m_TypeScale = 1.5f;
                        break;
                    }
            }
        }

        public override void UpdateMovement(float time)
        {
            var clampedTime = Mathf.Min(1.0f, time / Time);
            m_Offset = Vector3.up * Mathf.Max(0.0f, -Mathf.Sin(clampedTime * 3.0f * Mathf.PI)) * 0.96f;
            m_Scale.x = 1.0f + Mathf.Clamp(Mathf.Sin(clampedTime * 3.0f * Mathf.PI) * m_TypeScale, -1.0f, 1.0f) * 0.1f;
            m_Scale.y = 1.0f - Mathf.Clamp(Mathf.Sin(clampedTime * 3.0f * Mathf.PI) * m_TypeScale, -1.0f, 1.0f) * 0.1f;
            m_Scale.z = 1.0f + Mathf.Clamp(Mathf.Sin(clampedTime * 3.0f * Mathf.PI) * m_TypeScale, -1.0f, 1.0f) * 0.1f;
        }
    }
}