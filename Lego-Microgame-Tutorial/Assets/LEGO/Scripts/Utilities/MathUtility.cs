using UnityEngine;

namespace Unity.LEGO.Utilities
{

    public static class MathUtility
    {
        public static Quaternion ComputeHorizontalRotationDelta(Vector3 currentDirection, Vector3 desiredTargetDirection, float rotationSpeed)
        {
            // Project directions onto X-Z plane.
            desiredTargetDirection.y = 0.0f;
            currentDirection.y = 0.0f;

            // When directions are completely opposite, the rotation axis is not well defined.
            // Try to make it well defined, by rotating the target direction slightly.
            if (Vector3.Angle(currentDirection, desiredTargetDirection) > 179.0f)
            {
                desiredTargetDirection = Quaternion.Euler(0.0f, 0.1f, 0.0f) * desiredTargetDirection;
            }

            var desiredRotationDelta = Quaternion.FromToRotation(currentDirection, desiredTargetDirection);
            return Quaternion.RotateTowards(Quaternion.identity, desiredRotationDelta, rotationSpeed * Time.deltaTime);
        }

        public static Quaternion ComputeVerticalRotationDelta(Vector3 currentDirection, Vector3 desiredTargetDirection, float rotationSpeed)
        {
            // Rotate desiredTargetDirection into plane defined by current right and world up.
            var currentDirectionInPlane = currentDirection;
            currentDirectionInPlane.y = 0.0f;
            var desiredTargetDirectionInPlane = desiredTargetDirection;
            desiredTargetDirectionInPlane.y = 0.0f;
            var angleDiffInPlane = Vector3.SignedAngle(desiredTargetDirectionInPlane, currentDirectionInPlane, Vector3.up);
            desiredTargetDirection = Quaternion.Euler(0.0f, angleDiffInPlane, 0.0f) * desiredTargetDirection;

            // When directions are completely opposite, the rotation axis is not well defined.
            // Try to make it well defined, by rotating the target direction slightly.
            if (Vector3.Angle(currentDirection, desiredTargetDirection) > 179.0f)
            {
                desiredTargetDirection = Quaternion.Euler(0.0f, 0.0f, 0.1f) * desiredTargetDirection;
            }

            var desiredRotationDelta = Quaternion.FromToRotation(currentDirection, desiredTargetDirection);
            return Quaternion.RotateTowards(Quaternion.identity, desiredRotationDelta, rotationSpeed * Time.deltaTime);
        }

        public static float SignedAngleFromPlaneProjection(Vector3 directionToProject, Vector3 inPlaneDirection, Vector3 planeNormal)
        {
            var projection = Vector3.ProjectOnPlane(directionToProject, planeNormal);
            return Vector3.SignedAngle(projection, inPlaneDirection, planeNormal);
        }
    }
}
