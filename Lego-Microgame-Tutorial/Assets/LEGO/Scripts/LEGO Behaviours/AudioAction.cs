using UnityEngine;

namespace Unity.LEGO.Behaviours.Actions
{
    public class AudioAction : Action
    {
        [SerializeField, Tooltip("Play the audio in 3D. The player will only hear it when nearby.")]
        bool m_Spatial = false;

        [SerializeField, Tooltip("Play the audio continuously.")]
        bool m_Loop = true;

        float m_Time;
        bool m_IsPlaying;

        protected override void Reset()
        {
            base.Reset();

            m_AudioVolume = 0.5f;
            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Audio Action.png";
        }

        protected void Update()
        {
            if (m_Active)
            {
                if (!m_IsPlaying)
                {
                    PlayAudio(m_Loop, m_Spatial);
                    m_IsPlaying = true;
                }

                if (!m_Loop)
                {
                    m_Time += Time.deltaTime;
                    if (m_Time >= m_Audio.length)
                    {
                        m_IsPlaying = false;
                        m_Time = 0.0f;
                        m_Active = false;
                    }
                }
            }
        }
    }
}
