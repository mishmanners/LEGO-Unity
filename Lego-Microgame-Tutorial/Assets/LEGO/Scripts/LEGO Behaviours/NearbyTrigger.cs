using UnityEngine;

namespace Unity.LEGO.Behaviours.Triggers
{
    public class NearbyTrigger : SensoryTrigger
    {
        [SerializeField, Tooltip("The distance in LEGO modules.")]
        int m_Distance = 10;

        protected override void Reset()
        {
            base.Reset();

            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Nearby Trigger.png";
        }

        protected override void Start()
        {
            base.Start();

            if (IsPlacedOnBrick())
            {
                // Add one SensoryCollider based on scope pivot.
                var colliderComponentToClone = gameObject.AddComponent<SphereCollider>();
                colliderComponentToClone.center = m_ScopedPivotOffset;
                colliderComponentToClone.radius = 0.0f;
                colliderComponentToClone.enabled = false;

                var sensoryCollider = LEGOBehaviourCollider.Add<SensoryCollider>(colliderComponentToClone, m_ScopedBricks, m_Distance * LEGOBehaviour.LEGOHorizontalModule);
                SetupSensoryCollider(sensoryCollider);

                Destroy(colliderComponentToClone);
            }
        }
    }
}
