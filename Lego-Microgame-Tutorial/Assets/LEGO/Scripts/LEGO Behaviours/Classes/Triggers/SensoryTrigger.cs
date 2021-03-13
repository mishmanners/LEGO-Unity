using LEGOModelImporter;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.LEGO.Behaviours.Triggers
{
    public abstract class SensoryTrigger : Trigger
    {
        public enum Sense
        {
            Player,
            Bricks,
            Tag
        }

        [SerializeField, Tooltip("Trigger when sensing the player.\nor\nTrigger when sensing other bricks.\nor\nTrigger when sensing a tag.")]
        protected Sense m_Sense = Sense.Player;

        [SerializeField, Tooltip("The tag to sense.")]
        protected string m_SenseTag;

        protected HashSet<SensoryCollider> m_ActiveColliders = new HashSet<SensoryCollider>();

        void Update()
        {
            if (m_ActiveColliders.Count > 0)
            {
                ConditionMet();
            }
        }

        protected void SetupSensoryCollider(SensoryCollider collider)
        {
            collider.OnSensorActivated += SensoryColliderActivated;
            collider.OnSensorDeactivated += SensoryColliderDeactivated;

            collider.Sense = m_Sense;
            if (m_Sense == Sense.Tag)
            {
                collider.Tag = m_SenseTag;
            }
        }

        protected void SensoryColliderActivated(SensoryCollider collider, Collider _)
        {
            m_ActiveColliders.Add(collider);
        }

        protected void SensoryColliderDeactivated(SensoryCollider collider)
        {
            m_ActiveColliders.Remove(collider);
        }
    }
}
