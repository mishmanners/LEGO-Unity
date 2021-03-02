using UnityEngine;

namespace Unity.LEGO.Behaviours.Actions
{
    public class PlatformAction : MovementAction
    {
        [SerializeField, Tooltip("The distance in LEGO modules.")]
        int m_Distance = 15;

        enum State
        {
            MovingForward,
            WaitingToMoveBack,
            MovingBack,
            WaitingToMoveForward
        }

        State m_State;
        float m_NextMovementStartTime;
        float m_Offset;

        public Vector3 GetOffset()
        {
            return transform.forward * m_Offset;
        }

        protected override void Reset()
        {
            base.Reset();

            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Platform Action.png";
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

                // Move forward.
                if (m_State == State.MovingForward)
                {
                    if (IsColliding(transform.forward))
                    {
                        m_NextMovementStartTime = m_Time - m_CurrentTime + Time.fixedDeltaTime;
                        m_CurrentTime = Time.fixedDeltaTime;
                        m_State = State.WaitingToMoveBack;
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
                        m_Group.transform.position += transform.forward * delta;
                        m_Offset += delta;

                        // Update model position.
                        m_MovementTracker.UpdateModelPosition();

                        // Check if we are dome moving forward.
                        if (m_CurrentTime >= m_Time)
                        {
                            m_NextMovementStartTime = 0.0f;
                            m_CurrentTime -= m_Time;
                            m_State = State.WaitingToMoveBack;
                        }
                    }
                }

                // Waiting to move back.
                if (m_State == State.WaitingToMoveBack)
                {
                    if (m_CurrentTime >= m_Pause)
                    {
                        m_CurrentTime -= m_Pause;
                        m_CurrentTime += m_NextMovementStartTime;
                        m_State = State.MovingBack;
                        m_PlayAudio = true;
                    }
                }

                // Move back.
                if (m_State == State.MovingBack)
                {
                    if (IsColliding(-transform.forward))
                    {
                        m_NextMovementStartTime = m_Time - m_CurrentTime + Time.fixedDeltaTime;
                        m_CurrentTime = Time.fixedDeltaTime;
                        m_State = State.WaitingToMoveForward;
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
                        var delta = Mathf.Max(0, m_Distance - m_Distance / m_Time * m_CurrentTime) * LEGOHorizontalModule - m_Offset;
                        m_Group.transform.position += transform.forward * delta;
                        m_Offset += delta;

                        // Update model position.
                        m_MovementTracker.UpdateModelPosition();

                        // Check if we are done moving back.
                        if (m_CurrentTime >= m_Time)
                        {
                            m_NextMovementStartTime = 0.0f;
                            m_CurrentTime -= m_Time;
                            m_State = State.WaitingToMoveForward;
                        }
                    }
                }

                // Waiting to move forward.
                if (m_State == State.WaitingToMoveForward)
                {
                    if (m_CurrentTime >= m_Pause)
                    {
                        m_CurrentTime -= m_Pause;
                        m_CurrentTime += m_NextMovementStartTime;
                        m_State = State.MovingForward;
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
