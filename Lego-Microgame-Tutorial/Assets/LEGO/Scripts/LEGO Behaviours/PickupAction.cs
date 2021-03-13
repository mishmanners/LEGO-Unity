using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.LEGO.Behaviours.Triggers;

namespace Unity.LEGO.Behaviours.Actions
{
    public class PickupAction : Action
    {
        public Action<PickupAction> OnCollected;

        float m_InitialHoverOffset;
        Vector3 m_Offset;
        bool m_Initialised;
        bool m_Collected;

        List<LEGOBehaviour> m_Behaviours = new List<LEGOBehaviour>();

        const int k_AngularSpeed = 180;
        const int k_HoverSpeed = 2;
        const float k_HoverAmplitude = 2 * LEGOVerticalModule;
        const float k_HoverOffset = 2 * LEGOVerticalModule;

        protected HashSet<SensoryCollider> m_ActiveColliders = new HashSet<SensoryCollider>();

        [SerializeField, Tooltip("The effect used by the pickup.")]
        ParticleSystem m_Effect = null;

        ParticleSystem m_ParticleSystem;
        int m_BurstParticleCount;

        const int k_MinParticleEmission = 3;
        const int k_MaxParticleEmission = 500;
        const float k_ParticleEmissionPerModule = 3.0f;

        const int k_MinParticleBurst = 50;
        const int k_MaxParticleBurst = 200;
        const float k_ParticleBurstPerModule = 4.0f;

        protected override void Reset()
        {
            base.Reset();

            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Pickup Action.png";
        }

        protected override void Start()
        {
            base.Start();

            if (IsPlacedOnBrick())
            {
                // Add particle system.
                if (m_Effect)
                {
                    m_ParticleSystem = Instantiate(m_Effect, transform);
                    m_ParticleSystem.transform.position = m_ScopedBounds.center;
                    m_ParticleSystem.transform.localScale = m_ScopedBounds.size;
                    m_ParticleSystem.Stop();

                    // Scale particle emission with volume of scope.
                    var scopeVolume = m_ScopedBounds.size.x * m_ScopedBounds.size.y * m_ScopedBounds.size.z;
                    var particleEmissionModule = m_ParticleSystem.emission;
                    particleEmissionModule.rateOverTime = Mathf.Clamp(k_ParticleEmissionPerModule * scopeVolume / LEGOModuleVolume, k_MinParticleEmission, k_MaxParticleEmission);

                    // Scale particle burst when picked up with volume of scope.
                    m_BurstParticleCount = Mathf.RoundToInt(Mathf.Clamp(k_ParticleBurstPerModule * scopeVolume / LEGOModuleVolume, k_MinParticleBurst, k_MaxParticleBurst));
                }

                // Add SensoryCollider to all brick colliders.
                foreach (var brick in m_ScopedBricks)
                {
                    foreach (var part in brick.parts)
                    {
                        foreach (var collider in part.colliders)
                        {
                            var sensoryCollider = LEGOBehaviourCollider.Add<SensoryCollider>(collider, m_ScopedBricks, 0.64f);
                            SetupSensoryCollider(sensoryCollider);

                            // Make the original collider a trigger.
                            collider.isTrigger = true;
                        }
                    }
                }

                // Disconnect from all bricks not in scope.
                foreach (var brick in m_ScopedBricks)
                {
                    brick.DisconnectInverse(m_ScopedBricks);
                }

                // Make invisible.
                foreach (var partRenderer in m_scopedPartRenderers)
                {
                    partRenderer.enabled = false;
                }

                // Find all LEGOBehaviours in scope.
                foreach (var brick in m_ScopedBricks)
                {
                    m_Behaviours.AddRange(brick.GetComponentsInChildren<LEGOBehaviour>());
                }

                // Set random initial hover offset to desynchronise Pickup Actions.
                m_InitialHoverOffset = UnityEngine.Random.Range(0f, 360f);

                // Set random initial rotation to desynchronise Pickup Actions.
                float initialRotaion = UnityEngine.Random.Range(0f, 360f);
                var worldPivot = transform.position + transform.TransformVector(m_BrickPivotOffset);
                foreach (var brick in m_ScopedBricks)
                {
                    brick.transform.RotateAround(worldPivot, Vector3.up, initialRotaion);
                }
            }
        }

        protected void Update()
        {
            if (m_Active)
            {
                if (!m_Initialised)
                {
                    // Make visible.
                    foreach (var partRenderer in m_scopedPartRenderers)
                    {
                        partRenderer.enabled = true;
                    }

                    // Start particles.
                    if (m_ParticleSystem)
                    {
                        m_ParticleSystem.Play();
                    }

                    m_Initialised = true;
                }

                if (!m_Collected)
                {
                    // Move and rotate bricks,
                    var delta = Vector3.up * (k_HoverAmplitude * (Mathf.Sin((Time.time * 360f / k_HoverSpeed + m_InitialHoverOffset) * Mathf.Deg2Rad) / 2f + 0.5f) + k_HoverOffset) - m_Offset;
                    var worldPivot = transform.position + transform.TransformVector(m_BrickPivotOffset);
                    foreach (var brick in m_ScopedBricks)
                    {
                        brick.transform.position += delta;
                        brick.transform.RotateAround(worldPivot, Vector3.up, k_AngularSpeed * Time.deltaTime);
                    }
                    m_Offset += delta;

                    // Check if picked up.
                    if (m_ActiveColliders.Count > 0)
                    {
                        // Particle burst.
                        if (m_ParticleSystem)
                        {
                            m_ParticleSystem.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                            var particleMainModule = m_ParticleSystem.main;
                            var particleStartSpeed = particleMainModule.startSpeed;
                            particleStartSpeed.constantMin = 5.0f;
                            particleStartSpeed.constantMax = 10.0f;
                            particleMainModule.startSpeed = particleStartSpeed;
                            m_ParticleSystem.Emit(m_BurstParticleCount);
                        }

                        // Hide.
                        foreach (var partRenderer in m_scopedPartRenderers)
                        {
                            partRenderer.enabled = false;
                        }

                        PlayAudio(spatial: false, destroyWithAction: false);

                        // Delay destruction of LEGOBehaviours one frame to allow multiple Pickup Actions to be collected.
                        m_Collected = true;

                        OnCollected?.Invoke(this);
                    }
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

        protected void SetupSensoryCollider(SensoryCollider collider)
        {
            collider.OnSensorActivated += SensoryColliderActivated;
            collider.OnSensorDeactivated += SensoryColliderDeactivated;

            collider.Sense = SensoryTrigger.Sense.Player;
        }

        void SensoryColliderActivated(SensoryCollider collider, Collider _)
        {
            m_ActiveColliders.Add(collider);
        }

        void SensoryColliderDeactivated(SensoryCollider collider)
        {
            m_ActiveColliders.Remove(collider);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // Set original collider back to non-trigger if initialised and not collected.
            if (m_Initialised && !m_Collected)
            {
                foreach (var brick in m_ScopedBricks)
                {
                    foreach (var part in brick.parts)
                    {
                        foreach (var collider in part.colliders)
                        {
                            if (collider)
                            {
                                collider.isTrigger = false;
                            }
                        }
                    }
                }

                // Stop emitting particles.
                if (m_ParticleSystem)
                {
                    m_ParticleSystem.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }
    }
}
