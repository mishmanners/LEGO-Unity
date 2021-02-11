using Unity.LEGO.Utilities;
using UnityEngine;

namespace Unity.LEGO.Behaviours.Controls
{
    public class Aircraft : ControlMovement
    {
        const float k_VelocityBounceAmplification = 5.0f;
        const float k_RotationBounceRestitution = 1.2f;
        const float k_RollBankSpeedRatio = 0.33f; // Roll when banking at 33% of rotation speed.
        const float k_RollTurnSpeedRatio = 0.5f; // Roll when turning at 50% of rotation speed.
        const float k_RollbackSpeedRatio = 1.5f; // Roll back at 150% of angle difference per second.

        float m_RotationAngle;
        Vector3 m_RotationAxis;
        Vector3 m_RotationBounceAxis;

        public override void Movement(Vector3 targetDirection, float minSpeed, float maxSpeed, float idleSpeed)
        {
            var topSpeed = Mathf.Max(Mathf.Abs(minSpeed), Mathf.Abs(maxSpeed));

            // Compute target velocity.
            var targetVelocity = Vector3.up * targetDirection.y * topSpeed;
            targetDirection.y = 0.0f;
            var projectedTargetDirection = Vector3.Project(targetDirection, transform.forward);
            var sideDirection = targetDirection - projectedTargetDirection;

            if (Vector3.Dot(projectedTargetDirection, transform.forward) < 0.0f && (m_CameraAlignedRotation || !m_CameraRelativeMovement)) // If steering backwards and not a direct input type.
            {
                targetVelocity += projectedTargetDirection * (idleSpeed - minSpeed);
            }
            else
            {
                targetVelocity += projectedTargetDirection * (maxSpeed - idleSpeed);
            }
            targetVelocity += transform.forward * idleSpeed;
            targetVelocity += sideDirection * topSpeed;
            targetVelocity *= LEGOBehaviour.LEGOHorizontalModule;

            // Acceleration.
            var acceleration = topSpeed * 2.0f;
            m_Velocity = Acceleration(targetVelocity, m_Velocity, acceleration);
            m_CollisionVelocity = Acceleration(Vector3.zero, m_CollisionVelocity, acceleration);

            // Move bricks.
            m_Group.transform.position += (m_Velocity + m_CollisionVelocity) * Time.deltaTime;
        }

        public override void Rotation(Vector3 targetDirection, float rotationSpeed)
        {
            var worldPivot = transform.position + transform.TransformVector(m_BrickPivotOffset);

            RotationBounce(worldPivot, m_RotationBounceAxis);

            var bankSpeed = 0.0f;

            var forward = transform.forward;

            Vector3 desiredTargetDirection;

            if (m_CameraAlignedRotation) // Strafe.
            {
                desiredTargetDirection = m_MainCamera.transform.forward;
                bankSpeed = -Input.GetAxisRaw("Horizontal") * rotationSpeed * k_RollBankSpeedRatio;
            }
            else if (m_CameraRelativeMovement) // Direct.
            {
                desiredTargetDirection = targetDirection;
            }
            else // Tank.
            {
                var rotationAngle = Mathf.Clamp(Input.GetAxisRaw("Horizontal") * rotationSpeed, -179.0f, 179.0f);
                desiredTargetDirection = Quaternion.Euler(Vector3.up * rotationAngle) * forward;

                if (targetDirection.sqrMagnitude > 0.0f)
                {
                    if (Mathf.Abs(Vector3.Dot(targetDirection, Vector3.up)) == 1.0f)
                    {
                        desiredTargetDirection.y = targetDirection.y;
                    }
                    else
                    {
                        desiredTargetDirection.y = Mathf.Sign(Vector3.Dot(targetDirection, forward)) * targetDirection.y;
                    }
                }
            }

            ComputeRotation(out m_RotationAngle, out m_RotationAxis, desiredTargetDirection, rotationSpeed);

            // Roll back to level.
            var rollbackAngleDiff = -MathUtility.SignedAngleFromPlaneProjection(Vector3.up, transform.up, forward);
            var rollbackSpeed = rollbackAngleDiff * k_RollbackSpeedRatio;

            // Turn speed - used to roll into turns.
            var rotatedForward = Quaternion.AngleAxis(m_RotationAngle, m_RotationAxis) * forward;
            var turnAngle = MathUtility.SignedAngleFromPlaneProjection(forward, Vector3.ProjectOnPlane(rotatedForward, Vector3.up), Vector3.up);
            var turnSpeed = turnAngle / Time.deltaTime * k_RollTurnSpeedRatio;

            // Rolling rotation due to rolling back.
            m_Group.transform.RotateAround(worldPivot, forward, rollbackSpeed * Time.deltaTime);

            // Rolling rotation due to banking and turning.
            m_Group.transform.RotateAround(worldPivot, forward, (bankSpeed - turnSpeed) * Time.deltaTime);

            // Steering rotation.
            m_Group.transform.RotateAround(worldPivot, m_RotationAxis, m_RotationAngle);
        }

        public override void Collision(Vector3 direction)
        {
            if (Vector3.Dot(m_Velocity, direction) < 0.0f)
            {
                m_CollisionVelocity = -Vector3.Project(m_Velocity, direction) * 2.0f;
            }
            else
            {
                m_CollisionVelocity = -m_Velocity * 2.0f;
            }

            m_CollisionVelocity += direction * k_VelocityBounceAmplification;

            if (Mathf.Abs(m_RotationAngle) > 0.0f)
            {
                m_RotationBounceAngle = -m_RotationAngle * k_RotationBounceRestitution;
                m_RotationBounceAxis = m_RotationAxis;
                m_RotationBounceDamping = 1.0f;
            }
        }

        void ComputeRotation(out float rotationAngle, out Vector3 rotationAxis, Vector3 targetDirection, float rotationSpeed)
        {
            var currentDirection = transform.forward;

            Quaternion rotationDelta = MathUtility.ComputeHorizontalRotationDelta(currentDirection, targetDirection, rotationSpeed);
            rotationDelta *= MathUtility.ComputeVerticalRotationDelta(currentDirection, targetDirection, rotationSpeed);

            rotationDelta.ToAngleAxis(out rotationAngle, out rotationAxis);
            // Normalize rotation angle.
            rotationAngle = Mathf.Min(rotationSpeed * Time.deltaTime, rotationAngle);
        }
    }
}
