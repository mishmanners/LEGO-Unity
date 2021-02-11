using UnityEngine;

namespace Unity.LEGO.Behaviours.Actions
{
    public class MoveAction : MovementAction
    {
        [SerializeField, Tooltip("The distance in LEGO modules.")]
        int m_Distance = 15;

        enum State
        {
            Moving,
            WaitingToMove
        }

        State m_State;
        float m_Offset;

        public float GetRemainingDistance()
        {
            if (m_State == State.WaitingToMove || Mathf.Approximately(m_Time, 0.0f))
            {
                return m_Distance;
            }
            return m_Distance / m_Time * Mathf.Max(0.0f, m_Time - m_CurrentTime);
        }

        protected override void Reset()
        {
            base.Reset();

            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Move Action.png";
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            m_Distance = Mathf.Max(1, m_Distance);
        }

        void FixedUpdate()
        {
            if (m_Active)
            {
                // Update time.
                m_CurrentTime += Time.fixedDeltaTime;

                // Move.
                if (m_State == State.Moving)
                {
                    if (IsColliding())
                    {
                        m_CurrentTime = Time.fixedDeltaTime;
                        m_Offset = 0.0f;
                        m_State = State.WaitingToMove;
                    }
                    else
                    {
                        // Play audio.
                        if (m_PlayAudio)
                        {
                            PlayAudio();
                            m_PlayAudio = false;
                        }

                        // Move bricks.
                        var delta = Mathf.Min(m_Distance, m_Distance / m_Time * m_CurrentTime) * LEGOHorizontalModule - m_Offset;
                        var velocity = transform.forward * delta;
                        m_Group.transform.position += velocity;
                        m_Offset += delta;

                        // Update model position.
                        m_MovementTracker.UpdateModelPosition();

                        // Check if we are done moving.
                        if (m_CurrentTime >= m_Time)
                        {
                            m_Offset = 0.0f;
                            m_CurrentTime -= m_Time;
                            m_State = State.WaitingToMove;
                        }
                    }
                }

                // Waiting to move.
                if (m_State == State.WaitingToMove)
                {
                    if (m_CurrentTime >= m_Pause)
                    {
                        m_CurrentTime -= m_Pause;
                        m_State = State.Moving;
                        m_PlayAudio = true;
                        m_Active = m_Repeat;
                    }
                }
            }
        }

        protected override bool IsColliding()
        {
            if (base.IsColliding())
            {
                foreach (var activeColliderPair in m_ActiveColliderPairs)
                {
                    if (Physics.ComputePenetration(activeColliderPair.Item1, activeColliderPair.Item1.transform.position, activeColliderPair.Item1.transform.rotation,
                        activeColliderPair.Item2, activeColliderPair.Item2.transform.position, activeColliderPair.Item2.transform.rotation,
                        out Vector3 direction, out _))
                    {
                        if (Vector3.Dot(direction, transform.forward) < -0.0001f)
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
