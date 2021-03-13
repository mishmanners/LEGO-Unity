using LEGOModelImporter;
using System.Collections.Generic;
using UnityEngine;
using Unity.LEGO.Game;
using Unity.LEGO.Minifig;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.Behaviours
{
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField, Range(0.0f, 1080.0f), Tooltip("The rotation speed in degrees per second.")]
        float m_RotationSpeed = 0.0f;

        public bool Deadly { get; private set; } = true;

        Rigidbody m_RigidBody;
        CapsuleCollider m_Collider;
        ParticleSystem m_ParticleSystem;
        bool m_Rotate;
        Vector3 m_Rotation;
        HashSet<Brick> m_ScopedBricks;
        bool m_Launched;

        public void Init(HashSet<Brick> scopedBricks, float velocity, bool useGravity, float time)
        {
            m_ScopedBricks = scopedBricks;

            m_RigidBody.velocity = transform.forward * velocity;

            m_RigidBody.useGravity = useGravity;

            Destroy(gameObject, time);
        }

        void Awake()
        {
            m_Collider = GetComponent<CapsuleCollider>();

            // Disable the collider. We will enable it again once the projectile is clear of any initial colliders.
            // This ensures that the projectile will not collide with the Shoot Action that fires it.
            // Also, enabling the collider will ensure that OnTriggerEnter is fired even if the projectile is spawned completely inside a Trigger collider.
            m_Collider.enabled = false;

            m_RigidBody = GetComponent<Rigidbody>();

            m_RigidBody.isKinematic = false;
            m_RigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            m_ParticleSystem = GetComponent<ParticleSystem>();
            m_ParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (m_RotationSpeed > 0.0f)
            {
                m_Rotation = Random.onUnitSphere * m_RotationSpeed;
                m_Rotate = true;
            }
        }

        void Update()
        {
            // Check if the collider can be enabled.
            if (!m_Collider.enabled)
            {
                // Assumes that the capsule collider is aligned with local forward axis in projectile.
                var c0 = transform.TransformPoint(m_Collider.center - Vector3.forward * m_Collider.height * 0.5f);
                var c1 = transform.TransformPoint(m_Collider.center + Vector3.forward * m_Collider.height * 0.5f);
                var colliders = Physics.OverlapCapsule(c0, c1, m_Collider.radius);
                var collisions = false;
                foreach (var collider in colliders)
                {
                    // Do not collide with self, connectivity features, the player (if not a brick from the scope of the firing Shoot Action) or colliders from other LEGOBehaviourColliders.
                    if (collider != m_Collider &&
                        collider.gameObject.layer != LayerMask.NameToLayer(Connection.connectivityConnectorLayerName) &&
                        collider.gameObject.layer != LayerMask.NameToLayer(Connection.connectivityReceptorLayerName) &&
                        (!collider.gameObject.CompareTag("Player") || m_ScopedBricks.Contains(collider.GetComponentInParent<Brick>())) &&
                        !collider.GetComponent<LEGOBehaviourCollider>())
                    {
                        collisions = true;
                        break;
                    }
                }

                if (!collisions)
                {
                    m_Collider.enabled = true;
                }
            }

            // Play launch particle effect when projectile is no longer colliding with anything.
            if (!m_Launched && m_Collider.enabled)
            {
                m_ParticleSystem.Play();
                m_Launched = true;
            }

            if (Deadly)
            {
                if (m_Rotate)
                {
                    transform.Rotate(m_Rotation * Time.deltaTime);
                }
                else
                {
                    transform.rotation = Quaternion.LookRotation(m_RigidBody.velocity);
                }
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            // Check if the player was hit.
            if (Deadly && collision.collider.gameObject.CompareTag("Player"))
            {
                // If the player is a minifig or a brick, do an explosion.
                var minifigController = collision.collider.GetComponent<MinifigController>();
                if (minifigController)
                {
                    minifigController.Explode();
                }
                else
                {
                    var brick = collision.collider.GetComponentInParent<Brick>();
                    if (brick)
                    {
                        BrickExploder.ExplodeConnectedBricks(brick);
                    }
                }

                GameOverEvent evt = Events.GameOverEvent;
                evt.Win = false;
                EventManager.Broadcast(evt);
            }

            // Turn on gravity and make non-deadly.
            m_RigidBody.useGravity = true;

            Deadly = false;
        }
    }
}
