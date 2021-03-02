using LEGOModelImporter;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.LEGO.Behaviours
{
    public abstract class LEGOBehaviourCollider : MonoBehaviour
    {
        protected Collider m_BehaviourCollider;
        protected HashSet<Brick> m_IgnoredBricks = new HashSet<Brick>();

        public static T Add<T>(Collider collider, HashSet<Brick> ignoredBricks, float margin = 0.0f, int layer = -1) where T : LEGOBehaviourCollider
        {
            // Create a new game object underneath the collider.
            var parent = collider.gameObject.transform;

            var colliderGO = new GameObject("Behaviour Collider");
            colliderGO.transform.parent = parent;
            colliderGO.transform.localPosition = Vector3.zero;
            colliderGO.transform.localRotation = Quaternion.identity;
            if (layer >= 0)
            {
                colliderGO.layer = layer;
            }

            var colliderComponent = colliderGO.AddComponent<T>();

            // Make a copy of the collider, possibly add a margin, and set it as a trigger.
            var colliderType = collider.GetType();
            if (colliderType == typeof(BoxCollider))
            {
                var boxCollider = (BoxCollider)collider;

                var newBoxCollider = colliderGO.AddComponent<BoxCollider>();
                newBoxCollider.center = boxCollider.center;
                newBoxCollider.size = boxCollider.size + Vector3.one * 2.0f * margin;

                colliderComponent.m_BehaviourCollider = newBoxCollider;
            }
            else if (colliderType == typeof(SphereCollider))
            {
                var sphereCollider = (SphereCollider)collider;

                var newSphereCollider = colliderGO.AddComponent<SphereCollider>();
                newSphereCollider.center = sphereCollider.center;
                newSphereCollider.radius = sphereCollider.radius + margin;

                colliderComponent.m_BehaviourCollider = newSphereCollider;
            }

            colliderComponent.m_BehaviourCollider.isTrigger = true;
            colliderComponent.m_IgnoredBricks = ignoredBricks;

            // Add a rigid body component.
            var rigidBody = colliderGO.AddComponent<Rigidbody>();
            rigidBody.isKinematic = true;

            return colliderComponent;
        }
    }
}
