using LEGOMaterials;
using LEGOModelImporter;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.LEGO.Behaviours.Actions
{
    public abstract class MovementAction : RepeatableAction
    {
        [SerializeField, Tooltip("The time in seconds to complete each movement.")]
        protected float m_Time = 2.0f;

        [SerializeField, Tooltip("Stop when colliding with objects.")]
        protected bool m_Collide = true;

        protected float m_CurrentTime;
        protected bool m_PlayAudio = true;

        protected ModelGroup m_Group;
        protected MovementTracker m_MovementTracker;

        protected HashSet<(Collider, Collider)> m_ActiveColliderPairs = new HashSet<(Collider, Collider)>();

        HashSet<(Collider, Collider)> m_ColliderPairsToRemove = new HashSet<(Collider, Collider)>();

        protected override void Reset()
        {
            base.Reset();

            m_FlashColour = MouldingColour.GetColour(MouldingColour.Id.BrightBlue) * 2.0f;
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            m_Time = Mathf.Max(0.0f, m_Time);
        }

        protected override void Start()
        {
            base.Start();

            if (IsPlacedOnBrick())
            {
                // Store reference to group.
                m_Group = m_Brick.GetComponentInParent<ModelGroup>();

                // Add MovementTracker to model if not present already.
                var model = m_Brick.GetComponentInParent<Model>();
                m_MovementTracker = model.GetComponent<MovementTracker>();
                if (!m_MovementTracker)
                {
                    m_MovementTracker = model.gameObject.AddComponent<MovementTracker>();
                }
 
                // Add MovementCollider to all brick colliders.
                foreach (var brick in m_ScopedBricks)
                {
                    foreach (var part in brick.parts)
                    {
                        foreach (var collider in part.colliders)
                        {
                            var movementCollider = LEGOBehaviourCollider.Add<MovementCollider>(collider, m_ScopedBricks);
                            movementCollider.OnColliderActivated += MovementColliderActivated;
                            movementCollider.OnColliderDeactivated += MovementColliderDeactivated;
                        }
                    }
                }

                // Round the local rotation to prevent slight drifting between multiple opposite Movement Actions.
                transform.localRotation = Quaternion.Euler(Vector3Int.RoundToInt(transform.localRotation.eulerAngles));
            }
        }

        protected void MovementColliderActivated((Collider, Collider) colliderPair)
        {
            m_ActiveColliderPairs.Add(colliderPair);
        }

        protected void MovementColliderDeactivated((Collider, Collider) colliderPair)
        {
            m_ActiveColliderPairs.Remove(colliderPair);
        }

        protected virtual bool IsColliding()
        {
            m_ColliderPairsToRemove.Clear();

            foreach(var activeColliderPair in m_ActiveColliderPairs)
            {
                if (!activeColliderPair.Item2)
                {
                    m_ColliderPairsToRemove.Add(activeColliderPair);
                }
            }

            foreach(var colliderPairToRemove in m_ColliderPairsToRemove)
            {
                m_ActiveColliderPairs.Remove(colliderPairToRemove);
            }

            return m_Collide && m_ActiveColliderPairs.Count > 0;
        }
    }
}
