using System.Collections.Generic;
using UnityEngine;

namespace Unity.LEGO.Behaviours.Actions
{
    public class ExplodeAction : Action
    {
        [SerializeField, Range(1,50), Tooltip("The power of the explosion.")]
        uint m_Power = 10;

        bool m_Detonated;

        List<LEGOBehaviour> m_Behaviours = new List<LEGOBehaviour>();

        protected override void Reset()
        {
            base.Reset();

            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Explode Action.png";
        }

        protected override void Start()
        {
            base.Start();

            if (IsPlacedOnBrick())
            {
                // Find all LEGOBehaviours in scope.
                foreach (var brick in m_ScopedBricks)
                {
                    m_Behaviours.AddRange(brick.GetComponentsInChildren<LEGOBehaviour>());
                }
            }
        }

        protected void Update()
        {
            if (m_Active)
            {
                if (!m_Detonated)
                {
                    // Remove all game objects with LEGOBehaviourCollider components.
                    foreach (var brick in m_ScopedBricks)
                    {
                        foreach (var behaviourCollider in brick.GetComponentsInChildren<LEGOBehaviourCollider>())
                        {
                            Destroy(behaviourCollider.gameObject);
                        }

                        // Restore part's original colliders.
                        foreach(var part in brick.parts)
                        {
                            BrickColliderCombiner.RestoreOriginalColliders(part);
                        }
                    }

                    var lift = m_Power * 0.25f;

                    // Send all bricks in scope flying.
                    foreach (var brick in m_ScopedBricks)
                    {
                        brick.DisconnectAll();

                        var rigidBody = brick.gameObject.GetComponent<Rigidbody>();
                        if (!rigidBody)
                        {
                            rigidBody = brick.gameObject.AddComponent<Rigidbody>();
                        }
                        rigidBody.AddExplosionForce(m_Power, transform.position + transform.TransformVector(m_BrickPivotOffset), m_ScopedBounds.extents.magnitude, lift, ForceMode.VelocityChange);
                    }

                    PlayAudio(moveWithScope: false, destroyWithAction: false);

                    // Delay destruction of LEGOBehaviours one frame to allow multiple Explode Actions to detonate.
                    m_Detonated = true;
                }
                else
                {
                    // Destroy all the LEGOBehaviours in scope (including this script).
                    foreach (var behaviour in m_Behaviours)
                    {
                        if (behaviour)
                        {
                            Destroy(behaviour);
                        }
                    }
                }
            }
        }
    }
}
