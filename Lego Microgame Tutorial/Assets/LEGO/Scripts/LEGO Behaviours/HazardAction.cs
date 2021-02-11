using LEGOMaterials;
using LEGOModelImporter;
using System.Collections.Generic;
using UnityEngine;
using Unity.LEGO.Behaviours.Triggers;
using Unity.LEGO.Game;
using Unity.LEGO.Minifig;

namespace Unity.LEGO.Behaviours.Actions
{
    public class HazardAction : Action
    {
        [SerializeField, Tooltip("The effect used by the hazard.")]
        ParticleSystem m_Effect = null;

        protected HashSet<SensoryCollider> m_ActiveColliders = new HashSet<SensoryCollider>();
        Collider m_LastActivatingCollider;

        bool audioStarted;

        ParticleSystem m_ParticleSystem;

        List<Collider> m_EmissionColliders = new List<Collider>();

        float m_EmissionRate;
        float m_Emitted;

        // Approximated distribution function for weighted random selection of emission colliders when emitting particles.
        int m_EmissionColliderWeightSum;
        int[] m_EmissionColliderDistribution;

        const int k_MinParticleEmission = 1;
        const int k_MaxParticleEmission = 50;
        const float k_ParticleEmissionPerModule = 0.08f;

        protected override void Reset()
        {
            base.Reset();

            m_FlashColour = MouldingColour.GetColour(MouldingColour.Id.BrightRed) * 2.0f;
            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Hazard Action.png";
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
                    m_ParticleSystem.Stop();

                    var emissionColliderWeights = new List<int>();

                    // Scale particle emission with volume of scoped bricks.
                    var scopeVolume = 0.0f;
                    foreach (var brick in m_ScopedBricks)
                    {
                        foreach (var part in brick.parts)
                        {
                            foreach (var collider in part.colliders)
                            {
                                m_EmissionColliders.Add(collider);
                                var colliderType = collider.GetType();
                                if (colliderType == typeof(BoxCollider))
                                {
                                    var boxCollider = (BoxCollider)collider;
                                    var volume = boxCollider.size.x * boxCollider.size.y * boxCollider.size.z;
                                    var weight = Mathf.Max(1, Mathf.RoundToInt(volume));
                                    emissionColliderWeights.Add(weight);
                                    m_EmissionColliderWeightSum += weight;
                                    scopeVolume += volume;
                                }
                                else if (colliderType == typeof(SphereCollider))
                                {
                                    var sphereCollider = (SphereCollider)collider;
                                    var volume = 4.0f / 3.0f * Mathf.PI * sphereCollider.radius * sphereCollider.radius * sphereCollider.radius;
                                    var weight = Mathf.Max(1, Mathf.RoundToInt(volume));
                                    emissionColliderWeights.Add(weight);
                                    m_EmissionColliderWeightSum += weight;
                                    scopeVolume += volume;
                                }
                            }
                        }
                    }

                    m_EmissionRate = Mathf.Clamp(k_ParticleEmissionPerModule * scopeVolume / LEGOModuleVolume, k_MinParticleEmission, k_MaxParticleEmission);

                    // Compute an approximated distribution function for weighted random selection of emission colliders.
                    m_EmissionColliderDistribution = new int[m_EmissionColliderWeightSum];

                    var index = 0;
                    for (var i = 0; i < emissionColliderWeights.Count; ++i)
                    {
                        var colliderWeight = emissionColliderWeights[i];
                        for (var j = 0; j < colliderWeight; ++j)
                        {
                            m_EmissionColliderDistribution[index] = i;
                            index++;
                        }
                    }
                }

                // Add SensoryCollider to all brick colliders.
                foreach (var brick in m_ScopedBricks)
                {
                    foreach (var part in brick.parts)
                    {
                        foreach (var collider in part.colliders)
                        {
                            var hazardCollider = LEGOBehaviourCollider.Add<SensoryCollider>(collider, m_ScopedBricks, 0.64f, LayerMask.NameToLayer("Hazard"));
                            SetupSensoryCollider(hazardCollider);
                        }
                    }
                }
            }
        }

        protected void Update()
        {
            if (m_Active)
            {
                // Emit particles.
                if (m_ParticleSystem)
                {
                    var emissionCollider = m_EmissionColliders[m_EmissionColliderDistribution[Random.Range(0, m_EmissionColliderWeightSum)]];
                    var colliderType = emissionCollider.GetType();

                    if (emissionCollider)
                    {
                        m_ParticleSystem.transform.position = emissionCollider.transform.position;
                        m_ParticleSystem.transform.rotation = emissionCollider.transform.rotation;

                        var particleShapeMoule = m_ParticleSystem.shape;
                        if (colliderType == typeof(BoxCollider))
                        {
                            var boxCollider = (BoxCollider)emissionCollider;
                            particleShapeMoule.shapeType = ParticleSystemShapeType.BoxShell;
                            particleShapeMoule.position = boxCollider.center;
                            particleShapeMoule.scale = Quaternion.Euler(90.0f, 0.0f, 0.0f) * boxCollider.size;
                        }
                        else if (colliderType == typeof(SphereCollider))
                        {
                            var sphereCollider = (SphereCollider)emissionCollider;
                            particleShapeMoule.shapeType = ParticleSystemShapeType.Sphere;
                            particleShapeMoule.radiusThickness = 0.0f;
                            particleShapeMoule.position = sphereCollider.center;
                            particleShapeMoule.radius = sphereCollider.radius;
                        }

                        m_Emitted += m_EmissionRate * Time.deltaTime;
                        var emit = Mathf.FloorToInt(m_Emitted);
                        m_ParticleSystem.Emit(emit);
                        m_Emitted -= emit;
                    }
                }

                // Check if colliding with player.
                if (m_ActiveColliders.Count > 0)
                {
                    if (m_LastActivatingCollider)
                    {
                        // If the player is a minifig or a brick, do an explosion.
                        var minifigController = m_LastActivatingCollider.GetComponent<MinifigController>();
                        if (minifigController)
                        {
                            minifigController.Explode();
                        }
                        else
                        {
                            var brick = m_LastActivatingCollider.GetComponentInParent<Brick>();
                            if (brick)
                            {
                                BrickExploder.ExplodeConnectedBricks(brick);
                            }
                        }
                    }

                    GameOverEvent evt = Events.GameOverEvent;
                    evt.Win = false;
                    EventManager.Broadcast(evt);
                }

                if (!audioStarted)
                {
                    PlayAudio(true);
                    audioStarted = true;
                }
            }
        }

        protected void SetupSensoryCollider(SensoryCollider collider)
        {
            collider.OnSensorActivated += SensoryColliderActivated;
            collider.OnSensorDeactivated += SensoryColliderDeactivated;

            collider.Sense = SensoryTrigger.Sense.Player;
        }

        void SensoryColliderActivated(SensoryCollider collider, Collider activatingCollider)
        {
            m_LastActivatingCollider = activatingCollider;
            m_ActiveColliders.Add(collider);
        }

        void SensoryColliderDeactivated(SensoryCollider collider)
        {
            m_ActiveColliders.Remove(collider);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // Stop emitting particles.
            if (m_ParticleSystem)
            {
                m_ParticleSystem.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}