using UnityEngine;

namespace Unity.LEGO.Behaviours.Actions
{
    public class RotateAction : MovementAction
    {
        [SerializeField, Tooltip("Degrees to rotate.")]
        int m_Angle = 360;

        enum State
        {
            Rotating,
            WaitingToRotate
        }

        State m_State;
        float m_Offset;

        public float GetRemainingAngle()
        {
            if (m_State == State.WaitingToRotate || Mathf.Approximately(m_Time, 0.0f))
            {
                return m_Angle;
            }
            return m_Angle / m_Time * Mathf.Max(0.0f, m_Time - m_CurrentTime);
        }

        protected override void Reset()
        {
            base.Reset();

            m_Time = 5.0f;
            m_Pause = 0.0f;
            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Rotate Action.png";
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (m_Angle == 0)
            {
                m_Angle = 1;
            }
        }

        void FixedUpdate()
        {
            if (m_Active)
            {
                // Update time.
                m_CurrentTime += Time.fixedDeltaTime;

                // Rotate.
                if (m_State == State.Rotating)
                {
                    if (IsColliding())
                    {
                        m_CurrentTime = Time.fixedDeltaTime;
                        m_Offset = 0.0f;
                        m_State = State.WaitingToRotate;
                    }
                    else
                    {
                        // Play audio.
                        if (m_PlayAudio)
                        {
                            PlayAudio();
                            m_PlayAudio = false;
                        }

                        // Rotate bricks.
                        var delta = Mathf.Clamp(m_Angle / m_Time * m_CurrentTime, Mathf.Min(-m_Angle, m_Angle), Mathf.Max(-m_Angle, m_Angle)) - m_Offset;
                        var worldPivot = transform.position + transform.TransformVector(m_BrickPivotOffset);
                        m_Group.transform.RotateAround(worldPivot, transform.up, delta);
                        m_Offset += delta;

                        // Update model position.
                        m_MovementTracker.UpdateModelPosition();

                        // Check if we are done rotating.
                        if (m_CurrentTime >= m_Time)
                        {
                            m_Offset = 0.0f;
                            m_CurrentTime -= m_Time;
                            m_State = State.WaitingToRotate;
                        }
                    }
                }

                // Waiting to rotate.
                if (m_State == State.WaitingToRotate)
                {
                    if (m_CurrentTime >= m_Pause)
                    {
                        m_CurrentTime -= m_Pause;
                        m_State = State.Rotating;
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
                        // Attempt to find a point to represent the collision. This is an approximation.
                        Vector3 point;
                        var center = GetBrickCenter();
                        var colliderType = activeColliderPair.Item2.GetType();
                        if (colliderType == typeof(BoxCollider) || colliderType == typeof(SphereCollider) || colliderType == typeof(CapsuleCollider) || (colliderType == typeof(MeshCollider) && ((MeshCollider)activeColliderPair.Item2).convex))
                        {
                            point = activeColliderPair.Item2.ClosestPoint(center);
                        }
                        else
                        {
                            point = activeColliderPair.Item2.ClosestPointOnBounds(center);
                        }
                        point = activeColliderPair.Item1.ClosestPoint(point);

                        // Compute linear velocity of point as a result of the current rotation. This is again an approximation.
                        var delta = m_Angle / m_Time * Time.fixedDeltaTime;
                        var rotatedPoint = Quaternion.AngleAxis(delta, transform.up) * (point - center) + center;
                        var velocity = rotatedPoint - point;
                        if (Vector3.Dot(direction, velocity) < -0.0001f)
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
