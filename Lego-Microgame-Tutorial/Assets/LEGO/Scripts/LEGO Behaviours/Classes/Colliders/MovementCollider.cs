using LEGOModelImporter;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Unity.LEGO.Behaviours
{
    public class MovementCollider : LEGOBehaviourCollider
    {
        public Action<(Collider, Collider)> OnColliderActivated;
        public Action<(Collider, Collider)> OnColliderDeactivated;

        HashSet<(Collider, Collider)> m_ActiveTriggers = new HashSet<(Collider, Collider)>();

        void OnTriggerEnter(Collider other)
        {
            // Do not collide with triggers, projectiles or the player (if not a brick).
            if (!other.isTrigger &&
                !other.gameObject.CompareTag("Projectile") &&
                (!other.gameObject.CompareTag("Player") || other.GetComponentInParent<Brick>()))
            {
                // Do not collide with bricks in the ignored set. This is typically the scope of the MovementAction.
                if (!m_IgnoredBricks.Contains(other.GetComponentInParent<Brick>()))
                {
                    m_ActiveTriggers.Add((m_BehaviourCollider, other));
                    OnColliderActivated?.Invoke((m_BehaviourCollider, other));
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            // Do not collide with triggers, projectiles or the player (if not a brick).
            if (!other.isTrigger &&
                !other.gameObject.CompareTag("Projectile") &&
                (!other.gameObject.CompareTag("Player") || other.GetComponentInParent<Brick>()))
            {
                // Do not collide with bricks in the ignored set. This is typically the scope of the MovementAction.
                if (!m_IgnoredBricks.Contains(other.GetComponentInParent<Brick>()))
                {
                    m_ActiveTriggers.Remove((m_BehaviourCollider, other));
                    OnColliderDeactivated?.Invoke((m_BehaviourCollider, other));
                }
            }
        }

        void OnDestroy()
        {
            foreach(var activeTrigger in m_ActiveTriggers)
            {
                OnColliderDeactivated?.Invoke(activeTrigger);
            }
        }
    }
}
