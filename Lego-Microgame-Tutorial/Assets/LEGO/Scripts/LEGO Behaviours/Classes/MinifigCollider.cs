using System.Collections.Generic;
using UnityEngine;
using Unity.LEGO.Game;
using Unity.LEGO.Minifig;

namespace Unity.LEGO.Behaviours
{
    public class MinifigCollider : MonoBehaviour
    {
        class ColliderPair : IEqualityComparer<ColliderPair>
        {
            public Collider ColliderA;
            public Collider ColliderB;

            public bool Equals(ColliderPair x, ColliderPair y)
            {
                if (x.ColliderA == y.ColliderA && x.ColliderB == y.ColliderB) return true;
                if (x.ColliderB == y.ColliderA && x.ColliderA == y.ColliderB) return true;

                return false;
            }

            public int GetHashCode(ColliderPair x)
            {
                return x.ColliderA.GetHashCode() + x.ColliderB.GetHashCode();
            }
        }

        float m_HalfHeight;
        float m_Radius;
        Vector3 m_Center;

        MinifigController m_MinifigController;

        Vector3? m_PreviousAbovePosition;
        Vector3? m_PreviousBelowPosition;

        Dictionary<Collider, Vector3> m_PreviousSidePositions;

        void Start()
        {
            var controller = GetComponent<CharacterController>();
            m_HalfHeight = controller.height * 0.5f;
            m_Radius = controller.radius;
            m_Center = Vector3.up * m_HalfHeight;

            m_MinifigController = GetComponent<MinifigController>();
        }

        void Update()
        {
            var centerWS = transform.TransformPoint(m_Center);

            // Check above and below.
            RaycastHit aboveHit;
            var hitAbove = Physics.Raycast(centerWS, Vector3.up, out aboveHit, m_HalfHeight + 0.01f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            RaycastHit belowHit;
            var hitBelow = Physics.Raycast(centerWS, Vector3.down, out belowHit, m_HalfHeight + 0.01f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            // Ignore players and projectiles.
            if (hitAbove && hitBelow
                && !aboveHit.collider.CompareTag("Player") && !aboveHit.collider.CompareTag("Projectile")
                && !belowHit.collider.CompareTag("Player") && !belowHit.collider.CompareTag("Projectile")
            )
            {
                var currentAbovePosition = aboveHit.point;
                var currentBelowPosition = belowHit.point;

                if (m_PreviousAbovePosition.HasValue && m_PreviousBelowPosition.HasValue)
                {
                    var aboveGoingDown = Vector3.Dot(currentAbovePosition - m_PreviousAbovePosition.Value, Vector3.down) > 0.01f;
                    var belowGoingUp = Vector3.Dot(currentBelowPosition - m_PreviousBelowPosition.Value, Vector3.up) > 0.01f;

                    if (aboveGoingDown || belowGoingUp)
                    {
                        BreakMinifig();
                        return;
                    }
                }

                m_PreviousAbovePosition = currentAbovePosition;
                m_PreviousBelowPosition = currentBelowPosition;
            }
            else
            {
                m_PreviousAbovePosition = null;
                m_PreviousBelowPosition = null;
            }

            // Check sides.
            Dictionary<Collider, Vector3> currentSidePositions = new Dictionary<Collider, Vector3>();

            var sideColliders = Physics.OverlapSphere(centerWS, m_Radius + 0.01f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (var collider in sideColliders)
            {
                // Ignore players and projectiles.
                if (!collider.CompareTag("Player") && !collider.CompareTag("Projectile"))
                {
                    var colliderType = collider.GetType();
                    if (colliderType == typeof(BoxCollider) || colliderType == typeof(SphereCollider) || colliderType == typeof(CapsuleCollider) || (colliderType == typeof(MeshCollider) && ((MeshCollider)collider).convex))
                    {
                        currentSidePositions.Add(collider, collider.ClosestPoint(centerWS));
                    }
                    else
                    {
                        currentSidePositions.Add(collider, collider.ClosestPointOnBounds(centerWS));
                    }
                }
            }

            var oppositePairs = new HashSet<ColliderPair>();

            // 1. Check if we can find pairs of colliders in sufficiently opposite directions.
            foreach (var currentPosition in currentSidePositions)
            {
                var positionA = currentPosition.Value;
                var directionA = (positionA - centerWS).normalized;
                foreach (var otherCurrentPosition in currentSidePositions)
                {
                    if (currentPosition.Key == otherCurrentPosition.Key)
                    {
                        continue;
                    }

                    var positionB = otherCurrentPosition.Value;
                    var directionB = (positionB - centerWS).normalized;

                    if (Vector3.Dot(directionA, directionB) < -0.01f)
                    {
                        oppositePairs.Add(new ColliderPair { ColliderA = currentPosition.Key, ColliderB = otherCurrentPosition.Key });
                    }
                }
            }

            // 2. For each pair, check if one of them is moving sufficiently towards center.
            foreach (var oppositePair in oppositePairs)
            {
                if (m_PreviousSidePositions.ContainsKey(oppositePair.ColliderA))
                {
                    var position = currentSidePositions[oppositePair.ColliderA];
                    var velocity = (position - m_PreviousSidePositions[oppositePair.ColliderA]);
                    // We do not care about vertical component.
                    velocity.y = 0.0f;
                    if (velocity.sqrMagnitude > 0.0f)
                    {
                        var direction = (position - centerWS);
                        direction.y = 0.0f;

                        if (direction.sqrMagnitude > 0.0f)
                        {
                            if (direction.sqrMagnitude > 1.0f)
                            {
                                direction.Normalize();
                            }
                            if (velocity.sqrMagnitude > 1.0f)
                            {
                                velocity.Normalize();
                            }

                            if (Vector3.Dot(direction, velocity) < -0.01f)
                            {
                                BreakMinifig();
                                return;
                            }
                        }
                    }
                }

                if (m_PreviousSidePositions.ContainsKey(oppositePair.ColliderB))
                {
                    var position = currentSidePositions[oppositePair.ColliderB];
                    var velocity = (position - m_PreviousSidePositions[oppositePair.ColliderB]);
                    // We do not care about vertical component.
                    velocity.y = 0.0f;
                    if (velocity.sqrMagnitude > 0.0f)
                    {
                        var direction = (position - centerWS);
                        direction.y = 0.0f;

                        if (direction.sqrMagnitude > 0.0f)
                        {
                            if (direction.sqrMagnitude > 1.0f)
                            {
                                direction.Normalize();
                            }
                            if (velocity.sqrMagnitude > 1.0f)
                            {
                                velocity.Normalize();
                            }

                            if (Vector3.Dot(direction, velocity) < -0.01f)
                            {
                                BreakMinifig();
                                return;
                            }
                        }
                    }
                }
            }

            m_PreviousSidePositions = currentSidePositions;
        }

        void BreakMinifig()
        {
            m_MinifigController.Explode();

            GameOverEvent evt = Events.GameOverEvent;
            evt.Win = false;
            EventManager.Broadcast(evt);

            Destroy(this);
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.collider.CompareTag("Projectile"))
            {
                if (hit.collider.GetComponent<Projectile>().Deadly)
                {
                    BreakMinifig();
                }
            }
        }
    }
}
