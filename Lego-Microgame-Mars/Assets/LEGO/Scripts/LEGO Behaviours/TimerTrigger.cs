using UnityEngine;

namespace Unity.LEGO.Behaviours.Triggers
{
    public class TimerTrigger : Trigger
    {
        [SerializeField, Tooltip("The time in seconds before triggering.")]
        float m_Time = 10.0f;

        float m_CurrentTime;
        int m_PreviousProgress;

        public float GetElapsedRatio()
        {
            return !m_Repeat && m_AlreadyTriggered ? 1.0f : m_CurrentTime / m_Time;
        }

        protected override void Reset()
        {
            base.Reset();

            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Timer Trigger.png";
        }

        protected void OnValidate()
        {
            m_Time = Mathf.Max(0.0f, m_Time);
        }

        protected override void Start()
        {
            base.Start();

            Goal = Mathf.FloorToInt(m_Time);
        }

        void Update()
        {
            m_CurrentTime += Time.deltaTime;

            if (!m_AlreadyTriggered)
            {
                Progress = Mathf.FloorToInt(m_CurrentTime);
            }

            if (m_CurrentTime >= m_Time)
            {
                ConditionMet();

                m_CurrentTime -= m_Time;
            }
            else
            {
                if (m_PreviousProgress != Progress)
                {
                    OnProgress?.Invoke();
                }
            }

            m_PreviousProgress = Progress;
        }
    }
}
