using UnityEngine;

namespace Unity.LEGO.Behaviours.Triggers
{
    public class RandomTrigger : Trigger
    {
        [SerializeField, Tooltip("The min time in seconds before triggering.")]
        float m_MinTime;

        [SerializeField, Tooltip("The max time in seconds before triggering.")]
        float m_MaxTime = 10.0f;

        float m_Time;
        float m_CurrentTime;

        public float GetElapsedRatio()
        {
            return (!m_Repeat && m_AlreadyTriggered ? 1.0f : m_CurrentTime / m_Time);
        }

        protected override void Reset()
        {
            base.Reset();

            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Random Trigger.png";
        }

        protected void OnValidate()
        {
            m_MinTime = Mathf.Max(0.0f, m_MinTime);
            m_MaxTime = Mathf.Max(m_MinTime, m_MaxTime);
        }

        protected override void Start()
        {
            base.Start();

            m_Time = Random.Range(m_MinTime, m_MaxTime);
        }

        void Update()
        {
            m_CurrentTime += Time.deltaTime;

            if (m_CurrentTime >= m_Time)
            {
                ConditionMet();

                m_CurrentTime -= m_Time;
                m_Time = Random.Range(m_MinTime, m_MaxTime);
            }
        }
    }
}
