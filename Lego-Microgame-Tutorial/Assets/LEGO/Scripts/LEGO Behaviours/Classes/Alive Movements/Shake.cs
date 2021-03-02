using UnityEngine;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.Behaviours
{
    public class Shake : AliveMovement
    {
        float m_Angle;

        public Shake() : base(0.9f)
        {
        }

        public override void Initialise(Bounds bounds, AliveAction.Type type)
        {
            base.Initialise(bounds, type);

            m_Angle = Random.Range(4.0f, 8.0f);

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
            m_Rotation = Vector3.up * Mathf.Clamp(Mathf.Sin(clampedTime * 6.0f * Mathf.PI) * m_TypeScale, -1.0f, 1.0f) * m_Angle;
        }
    }
}