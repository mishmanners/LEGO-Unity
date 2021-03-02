using LEGOMaterials;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.LEGO.Behaviours.Triggers;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Unity.LEGO.Behaviours.Actions
{
    public abstract class Action : LEGOBehaviour
    {
        [SerializeField, Tooltip("The audio clip used by the Behaviour.")]
        protected AudioClip m_Audio;

        [SerializeField, Range(0.0f, 1.0f), Tooltip("The volume of the audio.")]
        protected float m_AudioVolume = 1.0f;

        protected bool m_Active;

        readonly List<AudioSource> m_AudioSourcesToDestroy = new List<AudioSource>();

        const float k_AudioRange = 80.0f;

        public virtual void Activate()
        {
            if (IsPlacedOnBrick())
            {
                if (!m_Active)
                {
                    m_Active = true;
                    Flash();
                }
            }
        }

        public List<Trigger> GetTargetingTriggers()
        {
            var result = new List<Trigger>();

#if UNITY_EDITOR
            var triggers = StageUtility.GetStageHandle(gameObject).FindComponentsOfType<Trigger>();
#else
            var triggers = FindObjectsOfType<Trigger>();
#endif

            foreach (var trigger in triggers)
            {
                if (trigger.GetTargetedActions().Contains(this))
                {
                    result.Add(trigger);
                }
            }

            return result;
        }

        protected override void Reset()
        {
            base.Reset();

            m_FlashColour = MouldingColour.GetColour(MouldingColour.Id.BrightGreen) * 2.0f;
        }

        protected virtual void Start()
        {
            if (IsPlacedOnBrick())
            {
                // If no triggers are targeting this action, activate itself.
                if (GetTargetingTriggers().Count == 0)
                {
                    Activate();
                }
            }
        }

        protected void PlayAudio(bool loop = false, bool spatial = true, bool moveWithScope = true, bool scopeDeterminesDistance = true, bool destroyWithAction = true)
        {
            if (m_Audio)
            {
                // Create new game object for audio source.
                GameObject go = new GameObject("Audio");
                if (moveWithScope)
                {
                    go.transform.parent = transform;
                }
                go.transform.position = transform.position + transform.TransformVector(m_ScopedPivotOffset);

                // Create audio source.
                AudioSource audioSource = go.AddComponent<AudioSource>();
                audioSource.clip = m_Audio;
                audioSource.loop = loop;
                audioSource.volume = m_AudioVolume;
                audioSource.dopplerLevel = 0.0f;
                if (spatial)
                {
                    audioSource.spatialBlend = 1.0f;
                    var minDistance = scopeDeterminesDistance ? Mathf.Max(5.0f, m_ScopedBounds.extents.magnitude) : 5.0f;
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
                }
                audioSource.Play();

                // Destroy game object when audio source is done playing.
                if (!loop)
                {
                    StartCoroutine(DoDestroyAudio(audioSource));
                }

                if (destroyWithAction)
                {
                    m_AudioSourcesToDestroy.Add(audioSource);
                }
            }
        }

        protected virtual void OnDestroy()
        {
            // Destroy audio sources.
            foreach (AudioSource audioSource in m_AudioSourcesToDestroy)
            {
                Destroy(audioSource.gameObject);
            }
        }

        IEnumerator DoDestroyAudio(AudioSource audioSource)
        {
            yield return new WaitForSeconds(audioSource.clip.length);

            m_AudioSourcesToDestroy.Remove(audioSource);

            Destroy(audioSource.gameObject);
        }
    }
}
