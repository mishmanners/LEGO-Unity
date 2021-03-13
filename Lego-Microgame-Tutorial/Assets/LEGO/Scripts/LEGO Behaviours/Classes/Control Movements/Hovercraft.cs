using UnityEngine;

namespace Unity.LEGO.Behaviours.Controls
{
    public class Hovercraft : ControlMovement
    {
        const float k_VelocityBounceAmplification = 3.0f;
        const float k_RotationBounceRestitution = 1.5f;

        float m_RotationSpeed;

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

            RotationBounce(worldPivot, Vector3.up);

            float angleDiff;
            var pointingDirection = new Vector3(transform.forward.x, 0.0f, transform.forward.z);

            m_RotationSpeed = rotationSpeed;

            if (m_CameraAlignedRotation) // Strafe.
            {
                var forwardXZ = new Vector3(m_MainCamera.transform.forward.x, 0.0f, m_MainCamera.transform.forward.z);
                angleDiff = Vector3.SignedAngle(pointingDirection, forwardXZ, Vector3.up);
            }
            else if (m_CameraRelativeMovement) // Direct.
            {
                var forwardXZ = new Vector3(targetDirection.x, 0.0f, targetDirection.z);
                angleDiff = Vector3.SignedAngle(pointingDirection, forwardXZ, Vector3.up);
            }
            else // Tank.
            {
                angleDiff = Input.GetAxisRaw("Horizontal") * m_RotationSpeed;
            }

            if (angleDiff < 0.0f)
            {
                m_RotationSpeed = -m_RotationSpeed;
            }

            // Assumes that x > NaN is false - otherwise we need to guard against Time.deltaTime being zero.
            if (Mathf.Abs(m_RotationSpeed) > Mathf.Abs(angleDiff) / Time.deltaTime)
            {
                m_RotationSpeed = angleDiff / Time.deltaTime;
            }

            // Rotate bricks.
            m_Group.transform.RotateAround(worldPivot, Vector3.up, m_RotationSpeed * Time.deltaTime);
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

            if (Mathf.Abs(m_RotationSpeed) > 0.0f)
            {
                m_RotationBounceAngle = -m_RotationSpeed * k_RotationBounceRestitution;
                m_RotationBounceDamping = 1.0f;
            }
        }
    }
}
