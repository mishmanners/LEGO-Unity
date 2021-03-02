// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LEGOModelImporter
{
    public class BrickBuildingUtility
    {
        /// <summary>
        /// One LEGO unit is 8mm, so that corresponds to 0.08 units in Unity units
        /// </summary>
        public const float LU_1 = 0.08f;
        public const float LU_5 = 5 * LU_1;
        public const float LU_10 = 10 * LU_1;

        static readonly Matrix4x4 LU_5_OFFSET = Matrix4x4.Translate(new Vector3(-LU_5, 0.0f, -LU_5));
        
        public static Collider[] colliderBuffer = new Collider[512];
        public static RaycastHit[] raycastBuffer = new RaycastHit[64];

        public const float maxRayDistance = 250.0f;

        // Defines the number of bricks to consider when finding best connection. Reduce to improve performance.
        public const int defaultMaxBricksToConsiderWhenFindingConnections = 3;

        // Defines the order of the neighbouring connection field positions when matching two connection fields.
        public static readonly Vector2[] connectionFieldOffsets =
            new Vector2[] {
                        new Vector2(0.0f, 0.0f), // No offset should always be first.
                        new Vector2(0.0f, 1.0f), new Vector2(0.0f, -1.0f),
                        new Vector2(1.0f, 0.0f), new Vector2(-1.0f, 0.0f),
                        new Vector2(1.0f, 1.0f), new Vector2(1.0f, -1.0f),
                        new Vector2(-1.0f, 1.0f), new Vector2(-1.0f, -1.0f)
            };
        
        /// <summary>
        /// Align a vector to LU in local space of a building plane.
        /// Offset by LU_5 in local space.
        /// </summary>
        /// <param name="position">The position to align. Is both input and output</param>
        /// <param name="transformation">A matrix whose local space we compute the alignment in</param>
        /// <param name="LU">The LU value to align to. Default value LU_5</param>
        /// <returns>The grid aligned vector aligned to given LU</returns>
        public static void AlignToGrid(ref Vector3 position, Matrix4x4 transformation, float LU = LU_5)
        {
            transformation = LU_5_OFFSET * transformation;
            var localPos = transformation.inverse.MultiplyPoint(position);
            localPos.x = Mathf.Round(localPos.x / LU) * LU;
            localPos.z = Mathf.Round(localPos.z / LU) * LU;
            position = transformation.MultiplyPoint(localPos);
        }

        /// <summary>
        /// Compute a grid-aligned position for the brick on intersecting geometry or on a pre-defined world plane
        /// </summary>
        /// <param name="ray">The ray to shoot into the scene and intersect with a plane</param>
        /// <param name="worldPlane">A fallback plane we want to intersect with/find a new position on if no geometry is hit</param>
        /// <param name="physicsScene">The physics scene we are working in</param>
        /// <param name="collidingHit">Out parameter for a raycast hit</param>
        /// <returns>The grid aligned position aligned to LU_5</returns>
        public static bool GetGridAlignedPosition(Ray ray, Plane worldPlane, PhysicsScene physicsScene, float maxDistance, out RaycastHit collidingHit)
        {
            var ignore = ~LayerMask.GetMask(Connection.connectivityReceptorLayerName, Connection.connectivityConnectorLayerName);
            var hits = physicsScene.Raycast(ray.origin, ray.direction, raycastBuffer, maxDistance, ignore, QueryTriggerInteraction.Ignore);
            if(hits > 0)
            {
                var shortestDistance = 10000.0f;
                var raycastHit = new RaycastHit();
                var hasHit = false;
                for(var i = 0; i < hits; i++)
                {
                    var hit = raycastBuffer[i];
                    var go = hit.collider.gameObject;
                    var brick = go.GetComponentInParent<Brick>();
                    if(brick == null)
                    {
                        var distance = Vector3.Distance(ray.origin, hit.point);
                        if(distance < shortestDistance)
                        {
                            hasHit = true;
                            shortestDistance = distance;
                            raycastHit = hit;
                        }
                    }   
                }

                if(hasHit)
                {
                    collidingHit = raycastHit;
                    return true;
                }
            }

            collidingHit = new RaycastHit();
            // Check if we hit the ground
            if (worldPlane.Raycast(ray, out float enter))
            {
                if(enter > maxDistance)
                {
                    return false;
                }
                var hitPoint = ray.GetPoint(enter);
                collidingHit.point = hitPoint;
                collidingHit.distance = enter;
                collidingHit.normal = worldPlane.normal;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check whether a transformation will result in a brick colliding with another brick
        /// </summary>
        /// <param name="brick">The brick to check</param>
        /// <param name="position">The position the brick will be checked in</param>
        /// <param name="rotation">The rotation the brick will be checked in</param>
        /// <param name="ignoreList">Optional list of bricks to ignore in the collision check</param>
        /// <returns></returns>
        public static bool IsCollidingAtTransformation(Brick brick, Vector3 position, Quaternion rotation, ICollection<Brick> ignoreList = null)
        {
            var oldPosition = brick.transform.position;
            var oldRotation = brick.transform.rotation;
            brick.transform.position = position;
            brick.transform.rotation = rotation;
            foreach (var part in brick.parts)
            {
                var isColliding = Part.IsColliding(part, colliderBuffer, out _, ignoreList);
                if (isColliding)
                {
                    brick.transform.rotation = oldRotation;
                    brick.transform.position = oldPosition;
                    return true;
                }
            }
            brick.transform.rotation = oldRotation;
            brick.transform.position = oldPosition;
            return false;
        }

        /// <summary>
        /// Compute the bounding corners of some bounds in some transformation
        /// </summary>
        /// <param name="bounds">The bounds to get the corners of</param>
        /// <param name="transformation">The transformation to transform the points into</param>
        /// <returns></returns>
        public static Vector3[] GetBoundingCorners(Bounds bounds, Matrix4x4 transformation)
        {
            var min = transformation.MultiplyPoint(bounds.min);
            var max = transformation.MultiplyPoint(bounds.max);

            var p0 = new Vector3(min.x, min.y, min.z);
            var p1 = new Vector3(max.x, min.y, min.z);
            var p2 = new Vector3(min.x, max.y, min.z);
            var p3 = new Vector3(min.x, min.y, max.z);

            var p4 = new Vector3(max.x, max.y, min.z);
            var p5 = new Vector3(max.x, min.y, max.z);
            var p6 = new Vector3(min.x, max.y, max.z);
            var p7 = new Vector3(max.x, max.y, max.z);
            
            var result = new Vector3[]{p0, p1, p2, p3, p4, p5, p6, p7};            

            return result;
        }

        /// <summary>
        /// Compute a min and max point for an array of points
        /// </summary>
        /// <param name="positions">The array of points</param>
        /// <param name="min">Out parameter for the minimum</param>
        /// <param name="max">Out parameter for the maximum</param>
        public static void GetMinMax(Vector3[] positions, out Vector3 min, out Vector3 max)
        {
            min = positions[0];
            max = positions[0];

            foreach(var position in positions)
            {
                min.x = Mathf.Min(position.x, min.x);
                min.y = Mathf.Min(position.y, min.y);
                min.z = Mathf.Min(position.z, min.z);

                max.x = Mathf.Max(position.x, max.x);
                max.y = Mathf.Max(position.y, max.y);
                max.z = Mathf.Max(position.z, max.z);
            }
        }

        /// <summary>
        /// Compute AABB bounds for a set of bricks
        /// </summary>
        /// <param name="bricks">The set of bricks</param>
        /// <param name="transformation">Transformation matrix to transform all bounds into</param>
        /// <returns>The AABB Bounds object for the bricks</returns>
        public static Bounds ComputeBounds(ICollection<Brick> bricks, Matrix4x4 transformation)
        {
            if(bricks.Count() == 0)
            {
                return new Bounds();
            }
            
            var brickEnumerator = bricks.GetEnumerator();
            brickEnumerator.MoveNext();

            var firstBrick = brickEnumerator.Current;

            // Get the bounding corners in world space
            var corners = GetBoundingCorners(firstBrick.totalBounds, transformation * firstBrick.transform.localToWorldMatrix);
            
            // Get actual min max in world space
            GetMinMax(corners, out Vector3 min, out Vector3 max);

            var totalBounds = new Bounds();
            totalBounds.min = min;
            totalBounds.max = max;

            while(brickEnumerator.MoveNext())
            {
                var brick = brickEnumerator.Current;

                corners = GetBoundingCorners(brick.totalBounds, transformation * brick.transform.localToWorldMatrix);
                GetMinMax(corners, out Vector3 minWS, out Vector3 maxWS);
                
                var bounds = new Bounds();
                bounds.min = minWS;
                bounds.max = maxWS;
                totalBounds.Encapsulate(bounds);
            }

            return totalBounds;
        }

        /// <summary>
        /// Compute bounds of list of bricks in world space
        /// </summary>
        /// <param name="bricks">The list of bricks</param>
        /// <returns></returns>
        public static Bounds ComputeBounds(ICollection<Brick> bricks)
        {
            return ComputeBounds(bricks, Matrix4x4.identity);
        }

        /// <summary>
        /// Computes the offset required to have all bricks above the plane defined by transformation
        /// </summary>
        /// <param name="focusBrickPosition">The position of the brick everything is relative to</param>
        /// <param name="bricks">The list of bricks to move</param>
        /// <param name="ray">The ray we are shooting into the scene</param>
        /// <param name="pickupOffset">The offset at which the focusBrick has been picked up</param>
        /// <param name="transformation">The transformation we want to do the computation in the space of</param>
        /// <returns></returns>
        public static Vector3 GetOffsetToGrid(Vector3 focusBrickPosition, HashSet<Brick> bricks, Ray ray, Vector3 pickupOffset, Matrix4x4 transformation)
        {
            // Compute the bounds in the local space of transformation
            var boundsLocal = ComputeBounds(bricks, transformation);

            // We only need the minimum
            var boundsMinLocal = boundsLocal.min;

            // Transform all necessary values into local space
            var rayOriginLocal = transformation.MultiplyPoint(ray.origin);
            var rayDirectionLocal = transformation.MultiplyVector(ray.direction);
            var focusPosLocal = transformation.MultiplyPoint(focusBrickPosition);
            var pickupOffsetLocal = transformation.MultiplyVector(pickupOffset);

            // To offset the ray we need to find exactly where we clicked on the brick in local space
            var pickupPosLocal = focusPosLocal + pickupOffsetLocal;
            var offsetToBoundsMin = pickupPosLocal - boundsMinLocal;

            // Offset ray origin
            var offsetOrigin = rayOriginLocal - offsetToBoundsMin;

            // Normally:
            // targetPos.y = rayT * rayDirection.y + offsetRayOrigin.y
            // => rayT = (targetPos.y - offsetRayOrigin.y) / rayDirection.y
            // But since we are always aligning to a local space, we always want y == 0.0f, so targetPos.y cancels out
            // leaving us with -offsetOrigin.y.
            var t = (-offsetOrigin.y) / rayDirectionLocal.y;

            // Compute the new position given t and ray
            var x = rayDirectionLocal.x * t + offsetOrigin.x;
            var z = rayDirectionLocal.z * t + offsetOrigin.z;
            var newPosLocal = new Vector3(x, 0.0f, z);
            
            // Compute offset
            return newPosLocal - boundsMinLocal;
        }

        ///<summary>
        ///Align a group of bricks on any intersecting geometry with a collider. Provide a fallback plane if nothing is hit
        ///</summary>
        ///<param name="sourceBrick">The brick we center this aligning around <paramref name="pickupOffset"/></param>
        ///<param name="bricks">The list of bricks</param>
        ///<param name="bounds">The current world space bounds of the bricks</param>
        ///<param name="pivot">The pivot used for rotating the bricks into an aligned orientation</param>
        ///<param name="pickupOffset">The offset at which we picked up the source brick</param>
        ///<param name="ray">The mouse ray we shoot into the scene</param>
        ///<param name="fallbackWorldPlane">A fallback plane if no geometry is hit</param>
        ///<param name="offset">Out parameter for the offset needed to place bricks on intersecting geometry</param>
        ///<param name="alignedOffset">Out parameter for the offset aligned to LU_10</param>
        ///<param name="rotation">Out parameter for the rotation needed to align</param>
        ///<param name="hit">Output parameter for hit information</param>
        public static void AlignBricks(Brick sourceBrick, HashSet<Brick> bricks, Bounds bounds, Vector3 pivot,
        Vector3 pickupOffset, Ray ray, Plane fallbackWorldPlane, float maxDistance ,out Vector3 offset, out Vector3 alignedOffset, out Quaternion rotation, out RaycastHit hit)
        {
            // Steps for placing selected bricks on intersecting geometry:
            // 1. Find a hit point (Raycast) on either geometry or fallback plane
            // 3. Find the closest axis of the focusbrick to the normal of the plane
            // 4. Orient all bricks around a pivot so that the found axis of the focusbrick aligns with the plane normal
            // 5. Compute bounds in the space of the plane (So they are properly aligned)
            // 6. Find out how much we need to offset to get above the grid/hit plane

            if(GetGridAlignedPosition(ray, fallbackWorldPlane, sourceBrick.gameObject.scene.GetPhysicsScene(), maxDistance, out hit))
            {                
                // Any hit will have a normal (either geometry hit or fallback plane)
                var normal = hit.normal;

                // Check all possible world axes of source brick transform and find the one that is
                // closest (by angle) to the plane normal

                // 1. Get rotation to align up with normal
                // 2. Cache rotation and apply aligned rotation to sourceBrick temporarily
                // 3. Get rotation needed to align with forward/right vectors
                // 4. Revert sourceBrick to cached rotation

                // Compute the rotation required to get the closest axis aligned to the plane normal
                var closestAxis = MathUtils.FindClosestAxis(sourceBrick.transform, normal, out MathUtils.VectorDirection vectorDirection);
                Quaternion rot = Quaternion.FromToRotation(closestAxis, normal);                
                var cachedSourceRot = sourceBrick.transform.rotation;
                sourceBrick.transform.rotation = rot * sourceBrick.transform.rotation;

                var axesToAlign = MathUtils.GetRelatedAxes(sourceBrick.transform, vectorDirection);

                // Compute the transformation matrix for the plane
                var origin = Vector3.zero;

                var up = Vector3.zero;
                var right = Vector3.zero;
                var forward = Vector3.zero;

                if(hit.collider != null)
                {
                    var hitTransform = hit.collider.transform;

                    // Find the axis closest to the normal on the transform
                    // to make sure we don't choose to align all bricks to the normal again
                    var hitRightAngle = Vector3.Angle(normal, hitTransform.right);
                    var hitUpAngle = Vector3.Angle(normal, hitTransform.up);
                    var hitForwardAngle = Vector3.Angle(normal, hitTransform.forward);
                    var hitLeftAngle = Vector3.Angle(normal, -hitTransform.right);
                    var hitDownAngle = Vector3.Angle(normal, -hitTransform.up);
                    var hitBackAngle = Vector3.Angle(normal, -hitTransform.forward);

                    var transformAxes = new List<Vector3>();

                    // Align the rotation of the transform to the normal
                    if(hitRightAngle <= hitUpAngle && hitRightAngle <= hitForwardAngle && hitRightAngle <= hitLeftAngle &&
                    hitRightAngle <= hitDownAngle && hitRightAngle <= hitBackAngle)
                    {
                        // normal points right
                        var fromTo = Quaternion.FromToRotation(hitTransform.right, normal);
                        transformAxes.Add(fromTo * hitTransform.up);
                        transformAxes.Add(fromTo * hitTransform.forward);
                    }
                    else if(hitUpAngle <= hitRightAngle && hitUpAngle <= hitForwardAngle && hitUpAngle <= hitLeftAngle &&
                    hitUpAngle <= hitDownAngle && hitUpAngle <= hitBackAngle)
                    {
                        // normal points up
                        transformAxes.Add(hitTransform.right);
                        transformAxes.Add(hitTransform.forward);
                    }
                    else if(hitForwardAngle <= hitRightAngle && hitForwardAngle <= hitUpAngle && hitForwardAngle <= hitLeftAngle &&
                    hitForwardAngle <= hitDownAngle && hitForwardAngle <= hitBackAngle)
                    {
                        // normal points forward
                        var fromTo = Quaternion.FromToRotation(hitTransform.forward, normal);
                        transformAxes.Add(fromTo * hitTransform.up);
                        transformAxes.Add(fromTo * hitTransform.right);
                    }
                    else if(hitLeftAngle <= hitUpAngle && hitLeftAngle <= hitForwardAngle && hitLeftAngle <= hitRightAngle &&
                    hitLeftAngle <= hitDownAngle && hitLeftAngle <= hitBackAngle)
                    {

                        // normal points left
                        var fromTo = Quaternion.FromToRotation(-hitTransform.right, normal);
                        transformAxes.Add(fromTo * -hitTransform.up);
                        transformAxes.Add(fromTo * -hitTransform.forward);
                    }
                    else if(hitDownAngle <= hitRightAngle && hitDownAngle <= hitForwardAngle && hitDownAngle <= hitLeftAngle &&
                    hitDownAngle <= hitUpAngle && hitDownAngle <= hitBackAngle)
                    {
                        // normal points down
                        var fromTo = Quaternion.FromToRotation(-hitTransform.up, normal);
                        transformAxes.Add(fromTo * -hitTransform.right);
                        transformAxes.Add(fromTo * -hitTransform.forward);
                    }
                    else if(hitBackAngle <= hitRightAngle && hitBackAngle <= hitUpAngle && hitBackAngle <= hitLeftAngle &&
                    hitBackAngle <= hitDownAngle && hitBackAngle <= hitForwardAngle)
                    {
                        // normal points back
                        var fromTo = Quaternion.FromToRotation(-hitTransform.forward, normal);
                        transformAxes.Add(fromTo * -hitTransform.up);
                        transformAxes.Add(fromTo * -hitTransform.right);
                    }

                    up = normal * hitTransform.localScale.y;
                    right = transformAxes[0] * hitTransform.localScale.x;
                    forward = transformAxes[1] * hitTransform.localScale.z;

                    var m = Matrix4x4.TRS(hitTransform.position, Quaternion.identity, Vector3.one);
                    m.SetColumn(0, forward);
                    m.SetColumn(1, up);
                    m.SetColumn(2, right);

                    rot = MathUtils.AlignRotation(new Vector3[]{axesToAlign.Item1, axesToAlign.Item2}, m) * rot;
                
                    // We want to find a common origin for all bricks hitting this specific transform
                    // The origin needs to align with some common point. It doesn't matter what that is, as long
                    // as it is common for this specific transform.
                    // The alignment should only happen in the XZ plane of the transform, which means alignment
                    // on the local Y-axis is always 0 in plane space.

                    var localOrigin = m.inverse.MultiplyPoint(hitTransform.position);
                    var localHit = m.inverse.MultiplyPoint(hit.point);

                    localOrigin.y = localHit.y;
                    origin = m.MultiplyPoint(localOrigin);
                }
                else
                {
                    rot = MathUtils.AlignRotation(new Vector3[]{axesToAlign.Item1, axesToAlign.Item2}, Matrix4x4.identity) * rot;
                    forward = Vector3.forward;
                    up = Vector3.up;
                    right = Vector3.right;
                    origin = new Vector3(0.0f, hit.point.y, 0.0f);
                }

                sourceBrick.transform.rotation = cachedSourceRot;
                
                rotation = rot;
                rot.ToAngleAxis(out float angle, out Vector3 axis);

                var oldPositions = new List<Vector3>();
                var oldRotations = new List<Quaternion>();

                foreach(var brick in bricks)
                {
                    oldPositions.Add(brick.transform.position);
                    oldRotations.Add(brick.transform.rotation);

                    brick.transform.RotateAround(pivot, axis, angle);
                }

                var planeTRS = Matrix4x4.TRS(origin, Quaternion.identity, Vector3.one);
                planeTRS.SetColumn(0, forward);
                planeTRS.SetColumn(1, up);
                planeTRS.SetColumn(2, right);
                
                // Now compute how much we need to offset the aligned brick selection to get it above the intersected area
                offset = GetOffsetToGrid(sourceBrick.transform.position, bricks, ray, pickupOffset, planeTRS.inverse);

                // Find out how far we are dragging the bricks along the ray to align with the plane.
                var localBoundsMin = planeTRS.inverse.MultiplyPoint(bounds.min);
                var boundsPos = localBoundsMin + offset;

                // If it is too far, drag it along the normal instead.
                if(Vector3.Distance(localBoundsMin, boundsPos) > 10.0f)
                {
                    var newRay = new Ray(hit.point + normal * 20.0f, -normal);
                    offset = GetOffsetToGrid(sourceBrick.transform.position, bricks, newRay, pickupOffset, planeTRS.inverse);
                }

                // Transform to world space
                offset = planeTRS.MultiplyVector(offset);

                var newPos = offset + sourceBrick.transform.position;                
                offset = newPos - sourceBrick.transform.position;

                var transformation = Matrix4x4.TRS(origin, Quaternion.identity, Vector3.one);
                transformation.SetColumn(0, forward.normalized);
                transformation.SetColumn(1, up.normalized);
                transformation.SetColumn(2, right.normalized);
                
                AlignToGrid(ref newPos, transformation, LU_10);
                alignedOffset = newPos - sourceBrick.transform.position;

                var j = 0;
                foreach(var brick in bricks)
                {
                    brick.transform.position = oldPositions[j];
                    brick.transform.rotation = oldRotations[j++];
                }
            }
            else
            {
                // In case there was no hit, get the offset required to place the bricks at a fixed distance along the ray
                var pointOnRay = ray.GetPoint(maxDistance);
                offset =  pointOnRay - sourceBrick.transform.position;
                AlignToGrid(ref pointOnRay, Matrix4x4.identity, LU_10);
                alignedOffset = pointOnRay - sourceBrick.transform.position;
                rotation = Quaternion.identity;
            }
        }

        private static List<Brick> CastBrick(HashSet<Brick> castBricks, Ray ray, Brick[] bricksToCheck)
        {
            List<Brick> bricks = new List<Brick>();

            //Notes:
            // Create the cone from the ray and total brick bounds of the selection.
            // "Cast" the cone into space.
            // For each brick in the scene, check its bounding sphere against the cone.
            // For each brick that is within the cone, add it to the list.
            // Return list.

            // Create cone.
            var totalBounds = ComputeBounds(castBricks);
            // Offset cone origin slightly forward to avoid intersecting with bricks too close to the origin of the ray.
            var coneOrigin = ray.origin + ray.direction * 3.0f;

            var distanceToBrick = Vector3.Distance(coneOrigin, totalBounds.center); // Use this to get the radius of the cone.
            // For a ray/cone axis r = origin + direction * t, where t is the distance to the center of the brick's bounding volume, the radius of the cone should be the extents of the brick to subtend.
            var radius = totalBounds.size.magnitude * 0.5f;
            var tanAngle = radius / distanceToBrick;
            var angle = Mathf.Atan(tanAngle);
            MathUtils.Cone cone = new MathUtils.Cone(coneOrigin, ray.direction, angle);

            // Cast into space and check bricks for overlap.
            foreach (var sceneBrick in bricksToCheck)
            {
                if(!sceneBrick.HasConnectivity())
                {
                    continue;
                }

                if (castBricks.Contains(sceneBrick))
                {
                    continue;
                }

                var totalBoundsForBrick = sceneBrick.totalBounds;

                if (totalBoundsForBrick.size.magnitude == 0)
                {
                    continue;
                }

                var sphereCenter = sceneBrick.transform.TransformPoint(totalBoundsForBrick.center);
                if (MathUtils.SphereIntersectCone(totalBoundsForBrick.size.magnitude * 0.5f, sphereCenter, cone))
                {
                    bricks.Add(sceneBrick);
                }
            }
            return bricks;
        }

        /// <summary>
        /// Finds the connection that best fits the given brick in a scene consisting of a list of given bricks
        /// </summary>
        /// <param name="pickupOffset">The offset at which we picked up the brick selection.false Relative to a single brick.</param>
        /// <param name="selectedBricks">The bricks we want to find a connection for</param>
        /// <param name="camera">The camera of the view</param>
        /// <param name="ray">The mouse ray for casting the bricks into the scene</param>
        /// <param name="bricksToCheck">A list of bricks we want to be able to connect to</param>
        /// <param name="maxTries">Optional parameter for amount of tries in finding a good connection</param>
        /// <returns></returns>
        public static (Connection, Connection) FindBestConnection(Vector3 pickupOffset, HashSet<Brick> selectedBricks, Ray ray, Camera camera, Brick[] bricksToCheck, int maxTries = defaultMaxBricksToConsiderWhenFindingConnections)
        {
            // Cast the brick into the scene as a cone with a radius approximately the size of the bounds of the brick
            var possibleBricks = CastBrick(selectedBricks, ray, bricksToCheck);

            if(possibleBricks.Count == 0)
            {
                return (null, null);
            }

            // FIXME Consider using a truncated cone as we can get intersections with stuff that is not visible due to being closer than the camera's near clip plane.

            // Sort the list of bricks by their z-distance in camera space.
            possibleBricks.Sort((b1, b2) =>
            {
                var p1 = camera.transform.InverseTransformPoint(b1.transform.TransformPoint(b1.totalBounds.center));
                var p2 = camera.transform.InverseTransformPoint(b2.transform.TransformPoint(b2.totalBounds.center));

                return p1.z.CompareTo(p2.z);
            });

            var selectedFields = new List<ConnectionField>();
            foreach(var selectedBrick in selectedBricks)
            {
                if(!selectedBrick.HasConnectivity())
                {
                    continue;
                }

                foreach (var part in selectedBrick.parts)
                {
                    if (part.connectivity == null)
                    {
                        continue;
                    }

                    foreach(var field in part.connectivity.connectionFields)
                    {
                        if(!field.HasAvailableConnections())
                        {
                            continue;
                        }
                        selectedFields.Add(field);
                    }
                }
            }

            var nonConnectivityBricks = new HashSet<Brick>();
            nonConnectivityBricks.UnionWith(selectedBricks);
            nonConnectivityBricks.RemoveWhere(x => !x.HasConnectivity());

            // Run through every brick we may connect to.
            // We need to consider more than just the closest brick since the sorting of possible bricks is not perfect.
            var bestConnectionCandidates = new List<(Connection, Connection)>();
            foreach (var brick in possibleBricks)
            {
                var bestConnectionOnBrick = FindBestConnectionOnBrick(pickupOffset, brick, ray, selectedFields, nonConnectivityBricks);
                if (bestConnectionOnBrick != (null, null))
                {
                    bestConnectionCandidates.Add(bestConnectionOnBrick);
                }

                // Stop when having found enough candidates.
                if (bestConnectionCandidates.Count >= maxTries)
                {
                    break;
                }
            }

            // Return the found connection that is closest to the camera.
            if (bestConnectionCandidates.Count > 0)
            {
                bestConnectionCandidates = bestConnectionCandidates.OrderBy(c =>
                {
                    var cPosition = c.Item2.field.GetPosition(c.Item2);
                    return Vector3.Distance(ray.origin, cPosition);
                }).ToList();

                return bestConnectionCandidates[0];
            }

            return (null, null);
        }

        /// <summary>
        /// Align the transformations of a list of bricks relative to a brick being transformed
        /// </summary>
        /// <param name="selectedBricks">The list of bricks to transform</param>
        /// <param name="pivot">The pivot for the rotation</param>
        /// <param name="axis">The axis we are rotating around</param>
        /// <param name="angle">The angle we are rotating by</param>
        /// <param name="offset">The offset we are moving the brick</param>
        public static void AlignTransformations(HashSet<Brick> selectedBricks, Vector3 pivot, Vector3 axis, float angle, Vector3 offset)
        {
            foreach(var selected in selectedBricks)
            {
                selected.transform.RotateAround(pivot, axis, angle);
                selected.transform.position += offset;
            }
        }

        static (Connection, Connection) FindBestConnectionOnBrick(Vector3 pickupOffset, Brick brick, Ray ray, List<ConnectionField> selectedFields, HashSet<Brick> selectedBricks)
        {
            var physicsScene = brick.gameObject.scene.GetPhysicsScene();
            var oldPositions = new List<Vector3>();
            var oldRotations = new List<Quaternion>();

            foreach(var selected in selectedBricks)
            {
                oldPositions.Add(selected.transform.position);
                oldRotations.Add(selected.transform.rotation);
            }

            foreach (var part in brick.parts)
            {
                if (part.connectivity == null)
                {
                    continue;
                }

                // Only pick from matching feature types
                var validConnectionPairs = new List<(ConnectionField, ConnectionField)>();

                foreach (var field in part.connectivity.connectionFields)
                {
                    if(!field.HasAvailableConnections())
                    {
                        continue;
                    }

                    foreach (var selectedField in selectedFields)
                    {
                        if(field.kind == selectedField.kind)
                        {
                            continue;
                        }

                        if (!ConnectionField.MatchTypes(field, selectedField))
                        {
                            continue;
                        }

                        validConnectionPairs.Add((field, selectedField));
                    }
                }

                if (validConnectionPairs.Count == 0)
                {
                    continue;
                }

                // Sort by distance to ray origin and then by angle of the up angle in case the distance is the same
                validConnectionPairs = validConnectionPairs.OrderBy(f =>
                {   
                    var field = f.Item1;
                    var fieldSize = new Vector3(field.gridSize.x, 0.0f, field.gridSize.y) * LU_5 * 0.5f;
                    var localCenter = new Vector3(-fieldSize.x, 0.0f, fieldSize.z);
                    var fieldCenter = field.transform.TransformPoint(localCenter);

                    return Vector3.Distance(ray.origin, fieldCenter);                                        
                }).ThenBy(f => {
                    return Vector3.Angle(f.Item1.transform.up, f.Item2.transform.up);
                }).ToList();

                // Run through every possible connectionfield
                foreach (var (field, selectedField) in validConnectionPairs)
                {    
                    var oldPos = selectedField.transform.position;
                    var oldRot = selectedField.transform.rotation;

                    Quaternion newRot;
                    if (!ConnectionField.AlignRotation(selectedField.transform, field.transform, out newRot))
                    {
                        continue;
                    }

                    var selectedBrick = selectedField.connectivity.part.brick;
                    var pivot = selectedBrick.transform.position + pickupOffset;

                    newRot.ToAngleAxis(out float angle, out Vector3 axis);
                    selectedField.transform.RotateAround(pivot, axis, angle);

                    // Project the selected field onto the static field along the ray direction
                    var selectedPos = selectedField.transform.position;
                    var localRay = field.transform.InverseTransformDirection(ray.direction);
                    var localOrigin = field.transform.InverseTransformPoint(ray.origin);
                    var localSelectedPos = field.transform.InverseTransformPoint(selectedPos);

                    var localProjectedT = (-Vector3.Dot(Vector3.up, localSelectedPos)) / Vector3.Dot(Vector3.up, localRay);
                    var localNewPoint = localSelectedPos + localRay * localProjectedT;

                    AlignToGrid(ref localNewPoint, Matrix4x4.identity);

                    // Check neighbouring positions in the order defined by connectionFieldOffsets.
                    // If we do not overlap with no offset, do not try the other offsets.
                    for (var i = 0; i < connectionFieldOffsets.Length; i++)
                    {
                        var offset = connectionFieldOffsets[i];
                        var localToWorld = field.transform.TransformPoint(localNewPoint - Vector3.right * offset.x * LU_5 + Vector3.forward * offset.y * LU_5);

                        selectedField.transform.position = localToWorld;

                        if(!ConnectionField.GetOverlap(field, selectedField, out Vector2Int min, out Vector2Int max))
                        {
                            selectedField.transform.position = oldPos;
                            selectedField.transform.rotation = oldRot;

                            // If we do not overlap with no offset, do not try the other offsets.
                            if (i == 0)
                            {
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        var currentFieldPos = selectedField.transform.position;
                        var currentFieldRot = selectedField.transform.rotation;

                        var reject = false;

                        List<(Vector2, Connection, Connection)> matches = new List<(Vector2, Connection, Connection)>();

                        // Check for rejections on the overlap
                        for(var x = min.x; x < max.x + 1; x++)
                        {
                            if(reject)
                            {
                                break;
                            }

                            for(var z = min.y; z < max.y + 1; z++)
                            {
                                if(reject)
                                {
                                    break;
                                }

                                var localPos = new Vector3(x * LU_5, 0.0f, z * LU_5);
                                var fieldConnection = field.GetConnectionAt(ConnectionField.ToGridPos(localPos));

                                if(fieldConnection != null && !field.HasConnection(fieldConnection))
                                {
                                    var fieldConnectionPosition = fieldConnection.field.GetPosition(fieldConnection);
                                    var connection = selectedField.GetConnectionAt(fieldConnectionPosition);                                    
                                    if(connection != null && !selectedField.HasConnection(connection))
                                    {
                                        var pairMatch = Connection.MatchTypes(fieldConnection.connectionType, connection.connectionType);
                                        if(pairMatch == Connection.ConnectionMatch.reject)
                                        {
                                            reject = true;
                                            break;
                                        }
                                        else if(pairMatch == Connection.ConnectionMatch.connect)
                                        {
                                            matches.Add((new Vector2(x, z), connection, fieldConnection));
                                        }
                                    }
                                }
                            }
                        }

                        if(reject)
                        {
                            continue;
                        }

                        foreach(var (pos, c1, c2) in matches)
                        {
                            if(reject)
                            {
                                break;
                            }

                            selectedField.transform.position = oldPos;
                            selectedField.transform.rotation = oldRot;

                            // Find the rotation and position we need to offset the other selected bricks with
                            ConnectionField.GetConnectedTransformation(c1, c2, pivot, out Vector3 connectedOffset, out angle, out axis);
                            
                            var oldBrickPos = selectedBrick.transform.position;
                            var oldBrickRot = selectedBrick.transform.rotation;

                            AlignTransformations(selectedBricks, pivot, axis, angle, connectedOffset);
                            Physics.SyncTransforms();

                            foreach(var checkBrick in selectedBricks)
                            {
                                if(reject)
                                {
                                    break;
                                }
                                
                                foreach(var checkPart in checkBrick.parts)
                                {
                                    if(reject)
                                    {
                                        break;
                                    }

                                    foreach(var checkField in checkPart.connectivity.connectionFields)
                                    {
                                        var cons = checkField.QueryConnections(out Connection.ConnectionMatch match, true);
                                        if(match == Connection.ConnectionMatch.reject)
                                        {
                                            selectedBrick.transform.position = oldBrickPos;
                                            selectedBrick.transform.rotation = oldBrickRot;
                                            ResetTransformations(selectedBricks, oldPositions, oldRotations, selectedField.connectivity.part.brick);
                                            reject = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            if(reject)
                            {
                                break;
                            }

                            Physics.SyncTransforms();

                            var colliding = Colliding(selectedBricks, selectedBrick);                            
                            
                            selectedBrick.transform.position = oldBrickPos;
                            selectedBrick.transform.rotation = oldBrickRot;

                            Physics.SyncTransforms();

                            if (!colliding && ConnectionField.IsConnectionValid(c1, c2, pivot, selectedBricks))
                            {
                                ResetTransformations(selectedBricks, oldPositions, oldRotations, selectedField.connectivity.part.brick);
                                return (c1, c2);
                            }
                            selectedField.transform.position = currentFieldPos;
                            selectedField.transform.rotation = currentFieldRot;
                            ResetTransformations(selectedBricks, oldPositions, oldRotations, selectedField.connectivity.part.brick);
                        }
                    }                   
                    selectedField.transform.position = oldPos;
                    selectedField.transform.rotation = oldRot;
                }
            }
            return (null, null);
        }

        private static bool Colliding(HashSet<Brick> bricks, Brick ignore)
        {
            foreach (var selected in bricks)
            {
                if (selected == ignore)
                {
                    continue;
                }

                foreach (var selectedPart in selected.parts)
                {
                    if (Part.IsColliding(selectedPart, colliderBuffer, out _, bricks))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static void ResetTransformations(HashSet<Brick> bricks, List<Vector3> positions, List<Quaternion> rotations, Brick ignore)
        {
            var j = 0;
            foreach(var selected in bricks)
            {
                if (selected != ignore)
                {
                    selected.transform.position = positions[j];
                    selected.transform.rotation = rotations[j];    
                }
                j++;
            }
        }
    }
}
