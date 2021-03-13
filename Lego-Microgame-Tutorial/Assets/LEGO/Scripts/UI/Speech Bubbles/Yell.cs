using TMPro;
using UnityEngine;

namespace Unity.LEGO.UI.SpeechBubbles
{
    public class Yell : MonoBehaviour, ISpeechBubble
    {
        [Header("References")]

        [SerializeField, Tooltip("The bubble image.")]
        GameObject m_Bubble;

        [SerializeField, Tooltip("The text that displays the speech.")]
        TextMeshProUGUI m_Text;

        [Header("Animation")]

        [SerializeField, Tooltip("The animation curve for scaling when activating.")]
        AnimationCurve m_ActivateScaleX;

        [SerializeField, Tooltip("The animation curve for scaling when activating.")]
        AnimationCurve m_ActivateScaleY;

        [SerializeField, Tooltip("The animation curve for scaling when deactivating.")]
        AnimationCurve m_DeactivateScaleX;

        [SerializeField, Tooltip("The animation curve for scaling when deactivating.")]
        AnimationCurve m_DeactivateScaleY;

        public TextMeshProUGUI Text { get { return m_Text; } }
        public float Height { get; } = 5.6f;
        public float TextDelay { get; } = 0.0f;
        public float DeactivationDuration { get; } = 0.4f;

        Vector3 m_DeactivationScale;

        enum State
        {
            Activating,
            Deactivating
        }
        State m_State = State.Activating;

        float m_Time;

        public void Activate()
        {
            gameObject.SetActive(true);

            m_State = State.Activating;

            m_Time = 0.0f;

            Update();
        }

        public void Deactivate()
        {
            if (m_State == State.Activating)
            {
                m_DeactivationScale = m_Bubble.transform.localScale;

                m_State = State.Deactivating;

                m_Time = 0.0f;
            }
        }

        void Awake()
        {
            gameObject.SetActive(false);
        }

        void Update()
        {
            m_Time += Time.deltaTime;

            if (m_State == State.Activating)
            {
                m_Bubble.transform.localScale = new Vector3(m_ActivateScaleX.Evaluate(m_Time), m_ActivateScaleY.Evaluate(m_Time), 1.0f);
            }

            if (m_State == State.Deactivating)
            {
                m_Bubble.transform.localScale = new Vector3(m_DeactivationScale.x * m_DeactivateScaleX.Evaluate(m_Time), m_DeactivationScale.y * m_DeactivateScaleY.Evaluate(m_Time), 1.0f);

                if (m_Time >= DeactivationDuration)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
