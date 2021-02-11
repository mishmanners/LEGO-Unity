using LEGOModelImporter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.LEGO.Behaviours
{

    public class BrickExploder : MonoBehaviour
    {
        [SerializeField, Tooltip("The audio clip used when exploding bricks.")]
        AudioClip m_Audio = default;

        const float k_AudioRange = 80.0f;

        static BrickExploder m_Instance;

        public static void ExplodeConnectedBricks(Brick brick)
        {
            if (m_Instance)
            {
                m_Instance.Explode(brick);
            }
        }

        void Awake()
        {
            if (m_Instance && m_Instance != this)
            {
                Destroy(this);
            }
            else
            {
                m_Instance = this;
            }
        }

        void Explode(Brick brick)
        {
            // Find connected bricks.
            var connectedBricks = brick.GetConnectedBricks();
            connectedBricks.Add(brick);

            // Find bounds of connected bricks.
            var connectedBounds = GetBounds(connectedBricks);

            // Remove all game objects with LEGOBehaviourCollider components.
            foreach (var connectedBrick in connectedBricks)
            {
                foreach (var behaviourCollider in connectedBrick.GetComponentsInChildren<LEGOBehaviourCollider>())
                {
                    Destroy(behaviourCollider.gameObject);
                }

                // Restore part's original colliders.
                foreach (var part in connectedBrick.parts)
                {
                    BrickColliderCombiner.RestoreOriginalColliders(part);
                }
            }

            // Destroy all LEGOBehaviours on connected bricks.
            foreach (var connectedBrick in connectedBricks)
            {
                foreach (var behaviour in connectedBrick.GetComponentsInChildren<LEGOBehaviour>())
                {
                    Destroy(behaviour);
                }
            }

            // Send connected bricks flying.
            foreach (var connectedBrick in connectedBricks)
            {
                connectedBrick.DisconnectAll();

                var rigidBody = connectedBrick.gameObject.GetComponent<Rigidbody>();
                if (!rigidBody)
                {
                    rigidBody = connectedBrick.gameObject.AddComponent<Rigidbody>();
                }
                rigidBody.AddExplosionForce(10.0f, connectedBounds.center, connectedBounds.extents.magnitude, 5.0f, ForceMode.VelocityChange);
            }

            // Play audio.
            PlayAudio(connectedBounds);
        }

        Bounds GetBounds(HashSet<Brick> connectedBricks)
        {
            var result = new Bounds();

            // Find the bounds and of the connectected bricks.
            var firstPartBounds = true;
            foreach (var brick in connectedBricks)
            {
                foreach (var part in brick.parts)
                {
                    var partRenderers = part.GetComponentsInChildren<MeshRenderer>(true);

                    foreach (var partRenderer in partRenderers)
                    {
                        if (firstPartBounds)
                        {
                            result = partRenderer.bounds;
                            firstPartBounds = false;
                        }
                        else
                        {
                            result.Encapsulate(partRenderer.bounds);
                        }
                    }
                }
            }

            return result;
        }

        void PlayAudio(Bounds bounds)
        {
            // Create new game object for audio source.
            GameObject go = new GameObject("Audio");
            go.transform.position = bounds.center;

            // Create audio source.
            AudioSource audioSource = go.AddComponent<AudioSource>();
            audioSource.clip = m_Audio;
            audioSource.loop = false;
            audioSource.volume = 1.0f;
            audioSource.dopplerLevel = 0.0f;
            audioSource.spatialBlend = 1.0f;
            var minDistance = Mathf.Max(5.0f, bounds.extents.magnitude);
            var maxDistance = minDistance + k_AudioRange;
            var minRatio = minDistance / maxDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff,
                new AnimationCurve(new Keyframe[] {
                        new Keyframe(minRatio, 1.0f, 0.0f, -3.0f),
                        new Keyframe(1.0f, 0.0f, 0.0f, 0.0f)
                })
            );

            // Play the audio.
            audioSource.Play();

            // Setup destruction of the audio source.
            StartCoroutine(DoDestroyAudio(audioSource));
        }

        IEnumerator DoDestroyAudio(AudioSource audioSource)
        {
            yield return new WaitForSeconds(audioSource.clip.length);

            Destroy(audioSource.gameObject);
        }
    }
}