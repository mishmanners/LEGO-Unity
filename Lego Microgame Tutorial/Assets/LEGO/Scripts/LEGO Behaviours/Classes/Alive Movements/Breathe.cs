using UnityEngine;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.Behaviours
{
    public class Breathe : AliveMovement
    {
        public Breathe() : base(1.25f)
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
                        m_TypeScale = 1.8f;
                        break;
                    }
            }
        }

        public override void UpdateMovement(float time)
        {
            var clampedTime = Mathf.Min(1.0f, time / Time);
            m_Scale.x = 1.0f + Mathf.Clamp(Mathf.Cos(Mathf.PI * 0.5f + clampedTime * 2.0f * Mathf.PI) * m_TypeScale, -1.0f, 1.0f) * 0.03f;
            m_Scale.y = 1.0f + Mathf.Clamp(Mathf.Sin(clampedTime * 4.0f * Mathf.PI) * m_TypeScale, -1.0f, 1.0f) * 0.045f;
            m_Scale.z = 1.0f + Mathf.Clamp(Mathf.Cos(Mathf.PI * 0.5f + clampedTime * 2.0f * Mathf.PI) * m_TypeScale, -1.0f, 1.0f) * 0.03f;
        }
    }
}