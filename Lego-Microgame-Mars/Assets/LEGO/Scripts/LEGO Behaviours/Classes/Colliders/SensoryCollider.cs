using LEGOModelImporter;
using System;
using System.Collections.Generic;
using Unity.LEGO.Behaviours.Triggers;
using UnityEngine;

namespace Unity.LEGO.Behaviours
{
    public class SensoryCollider : LEGOBehaviourCollider
    {
        public Action<SensoryCollider, Collider> OnSensorActivated;
        public Action<SensoryCollider> OnSensorDeactivated;

        public SensoryTrigger.Sense Sense;

        public string Tag;

        HashSet<Collider> m_ActiveTriggers = new HashSet<Collider>();

        void OnTriggerEnter(Collider other)
        {
            if (IsSensed(other))
            {
                if (m_ActiveTriggers.Count == 0)
                {
                    OnSensorActivated?.Invoke(this, other);
                }
                m_ActiveTriggers.Add(other);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (IsSensed(other))
            {
                m_ActiveTriggers.Remove(other);
                if (m_ActiveTriggers.Count == 0)
                {
                    OnSensorDeactivated?.Invoke(this);
                }
            }
        }

        void Update()
        {
            if (m_ActiveTriggers.Count > 0)
            {
                m_ActiveTriggers.RemoveWhere(activeTrigger => activeTrigger == null);
                if (m_ActiveTriggers.Count == 0)
                {
                    OnSensorDeactivated?.Invoke(this);
                }
            }
        }

        bool IsSensed(Collider collider)
        {
            // Do not collide with triggers.
            if (collider.isTrigger)
            {
                return false;
            }

            switch(Sense)
            {
                case SensoryTrigger.Sense.Player:
                    {
                        // If sensing player, check if collider belongs to player.
                        return collider.gameObject.CompareTag("Player");
                    }
                case SensoryTrigger.Sense.Bricks:
                    {
                        // If sensing bricks, first check for collision with projectiles.
                        if (collider.gameObject.CompareTag("Projectile"))
                        {
                            return true;
                        }

                        // If sensing bricks, do not collide with bricks in the ignored set. This is typically the scope of the SensoryTrigger.
                        var brick = collider.GetComponentInParent<Brick>();
                        if (m_IgnoredBricks.Contains(brick))
                        {
                            return false;
                        }

                        // If sensing bricks, check if colliding with brick.
                        return brick;
                    }
                case SensoryTrigger.Sense.Tag:
                    {
                        // If sensing a tag, check if collider has that tag.
                        return collider.gameObject.CompareTag(Tag);
                    }
            }

            return false;
        }

        void OnDestroy()
        {
            OnSensorDeactivated?.Invoke(this);
        }
    }
}
