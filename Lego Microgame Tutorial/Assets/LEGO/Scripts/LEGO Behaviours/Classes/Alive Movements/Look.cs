using UnityEngine;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.Behaviours
{
    public class Look : AliveMovement
    {
        float m_Angle;

        public Look() : base(1.5f)
        {
        }

        public override void Initialise(Bounds bounds, AliveAction.Type type)
        {
            base.Initialise(bounds, type);

            m_Angle = Random.Range(30.0f, 45.0f) * (Random.value > 0.5f ? 1.0f : -1.0f);

            switch(m_Type)
            {
                case AliveAction.Type.Creature:
                    {
                        m_TypeScale = 1.0f;
                        m_TypeClamp = 1.0f;
                        break;
                    }
                case AliveAction.Type.Robot:
                    {
                        m_TypeScale = 1.8f;
                        m_TypeClamp = 0.8f;
                        break;
                    }
            }
        }

        public override void UpdateMovement(float time)
        {
            var clampedTime = Mathf.Min(1.0f, time / Time);
            m_Rotation = Vector3.up * Mathf.Clamp(
                (Mathf.Sin(clampedTime * Mathf.PI) + Mathf.Sin(clampedTime * 3.0f * Mathf.PI) * 0.25f + Mathf.Sin(clampedTime * 5.0f * Mathf.PI) *
                0.125f) * m_TypeScale, -m_TypeClamp, m_TypeClamp)
                * m_Angle;
        }
    }
}