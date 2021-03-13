using UnityEngine;

namespace Unity.LEGO.Behaviours.Actions
{
    public class ElevatorAction : MovementAction
    {
        [SerializeField, Tooltip("The distance in LEGO modules.")]
        int m_Distance = 15;

        enum State
        {
            MovingUp,
            WaitingToMoveDown,
            MovingDown,
            WaitingToMoveUp
        }

        State m_State;
        float m_NextMovementStartTime;
        Vector3 m_Offset;

        public Vector3 GetOffset()
        {
            return m_Offset;
        }

        protected override void Reset()
        {
            base.Reset();

            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Elevator Action.png";
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

                // Move up.
                if (m_State == State.MovingUp)
                {
                    if (IsColliding(Vector3.up))
                    {
                        m_NextMovementStartTime = m_Time - m_CurrentTime + Time.fixedDeltaTime;
                        m_CurrentTime = Time.fixedDeltaTime;
                        m_State = State.WaitingToMoveDown;
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
                        var delta = Vector3.up * Mathf.Min(m_Distance, m_Distance / m_Time * m_CurrentTime) * LEGOVerticalModule - m_Offset;
                        m_Group.transform.position += delta;
                        m_Offset += delta;

                        // Update model position.
                        m_MovementTracker.UpdateModelPosition();

                        // Check if we are done moving up.
                        if (m_CurrentTime >= m_Time)
                        {
                            m_NextMovementStartTime = 0.0f;
                            m_CurrentTime -= m_Time;
                            m_State = State.WaitingToMoveDown;
                        }
                    }
                }

                // Waiting to move down.
                if (m_State == State.WaitingToMoveDown)
                {
                    if (m_CurrentTime >= m_Pause)
                    {
                        m_CurrentTime -= m_Pause;
                        m_CurrentTime += m_NextMovementStartTime;
                        m_State = State.MovingDown;
                        m_PlayAudio = true;
                    }
                }

                // Move down.
                if (m_State == State.MovingDown)
                {
                    if (IsColliding(Vector3.down))
                    {
                        m_NextMovementStartTime = m_Time - m_CurrentTime + Time.fixedDeltaTime;
                        m_CurrentTime = Time.fixedDeltaTime;
                        m_State = State.WaitingToMoveUp;
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
                        var delta = Vector3.up * Mathf.Max(0, m_Distance - m_Distance / m_Time * m_CurrentTime) * LEGOVerticalModule - m_Offset;
                        m_Group.transform.position += delta;
                        m_Offset += delta;

                        // Update model position.
                        m_MovementTracker.UpdateModelPosition();

                        // Check if we are done moving down.
                        if (m_CurrentTime >= m_Time)
                        {
                            m_NextMovementStartTime = 0.0f;
                            m_CurrentTime -= m_Time;
                            m_State = State.WaitingToMoveUp;
                        }
                    }
                }

                // Waiting to move up.
                if (m_State == State.WaitingToMoveUp)
                {
                    if (m_CurrentTime >= m_Pause)
                    {
                        m_CurrentTime -= m_Pause;
                        m_CurrentTime += m_NextMovementStartTime;
                        m_State = State.MovingUp;
                        m_PlayAudio = true;
                        m_Active = m_Repeat;
                    }
                }
            }
        }

        protected bool IsColliding(Vector3 movingDirection)
        {
            if (base.IsColliding())
            {
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
