using UnityEngine;

namespace Unity.LEGO.Behaviours.Actions
{
    public class HoverAction : MovementAction
    {
        [SerializeField, Tooltip("The vertical displacement in LEGO modules.")]
        int m_Amplitude = 1;

        Vector3 m_Offset;

        public Vector3 GetOffset()
        {
            return m_Offset;
        }

        protected override void Reset()
        {
            base.Reset();

            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Hover Action.png";
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            m_Amplitude = Mathf.Max(1, m_Amplitude);
            m_Time = Mathf.Max(0.1f, m_Time);
        }
      
        void FixedUpdate()
        {
            if (m_Active)
            {
                // Update time.
                m_CurrentTime += Time.fixedDeltaTime;

                // Handle collision.
                if (IsColliding())
                {
                    // Bounce wave.
                    var waveValue = (m_CurrentTime / m_Time * 2.0f * Mathf.PI) % (2.0f * Mathf.PI);
                    var bouncedWaveValue = 2.0f * Mathf.PI - waveValue + Mathf.PI;
                    m_CurrentTime = bouncedWaveValue * m_Time / 2.0f / Mathf.PI + 2 * Time.fixedDeltaTime;
                }

                // Move bricks.
                var delta = Vector3.up * m_Amplitude * Mathf.Sin(m_CurrentTime / m_Time * 2.0f * Mathf.PI) * LEGOVerticalModule - m_Offset;
                m_Group.transform.position += delta;
                m_Offset += delta;

                // Update model position.
                m_MovementTracker.UpdateModelPosition();
            }
        }

        protected override bool IsColliding()
        {
            if (base.IsColliding())
            {
                var movingDirection = Vector3.up * Mathf.Cos(m_CurrentTime / m_Time * 2.0f * Mathf.PI);

                foreach (var activeColliderPair in m_ActiveColliderPairs)
                {
                    if (Physics.ComputePenetration(activeColliderPair.Item1, activeColliderPair.Item1.transform.position, activeColliderPair.Item1.transform.rotation,
                        activeColliderPair.Item2, activeColliderPair.Item2.transform.position, activeColliderPair.Item2.transform.rotation,
                        out Vector3 seperatingDirection, out _))
                    {
                        if (Vector3.Dot(seperatingDirection, movingDirection) < -0.0001f)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
