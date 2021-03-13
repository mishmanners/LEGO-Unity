using TMPro;
using UnityEngine;

namespace Unity.LEGO.UI.SpeechBubbles
{
    public class Think : MonoBehaviour, ISpeechBubble
    {
        [Header("References")]

        [SerializeField, Tooltip("The tiny bubble image.")]
        GameObject m_Bubble1;

        [SerializeField, Tooltip("The small bubble image.")]
        GameObject m_Bubble2;

        [SerializeField, Tooltip("The main bubble image.")]
        GameObject m_Bubble3;

        [SerializeField, Tooltip("The text that displays the speech.")]
        TextMeshProUGUI m_Text;

        [SerializeField, Tooltip("The particle system that plays when deactivating.")]
        ParticleSystem m_Puff;

        [Header("Animation")]

        [SerializeField, Tooltip("The animation curve for scaling when activating.")]
        AnimationCurve m_ActivateScale;

        [SerializeField, Tooltip("The animation curve for alpha when activating.")]
        AnimationCurve m_ActivateAlpha;

        [SerializeField, Tooltip("The animation curve for scaling when deactivating.")]
        AnimationCurve m_DeactivateScale;

        [SerializeField, Tooltip("The animation curve for alpha when deactivating.")]
        AnimationCurve m_DeactivateAlpha;

        public TextMeshProUGUI Text { get { return m_Text; } }

        public float Height { get; } = 5.6f;
        public float TextDelay { get; } = 0.6f;
        public float DeactivationDuration { get; } = 1.1f;

        const float k_BubbleDelay = 0.3f;
        const float k_ScaleAmplitude = 0.05f;
        const float k_ScaleMinSpeed = 1.0f;
        const float k_ScaleMaxSpeed = 2.0f;

        float m_Bubble1ScaleSpeedX;
        float m_Bubble1ScaleSpeedY;
        float m_Bubble1ScaleTimeOffsetX;
        float m_Bubble1ScaleTimeOffsetY;
        Vector3 m_Bubble1DeactivationScale;
        float m_Bubble1DeactivationAlpha;

        float m_Bubble2ScaleSpeedX;
        float m_Bubble2ScaleSpeedY;
        float m_Bubble2ScaleTimeOffsetX;
        float m_Bubble2ScaleTimeOffsetY;
        Vector3 m_Bubble2DeactivationScale;
        float m_Bubble2DeactivationAlpha;

        float m_Bubble3ScaleSpeedX;
        float m_Bubble3ScaleSpeedY;
        float m_Bubble3ScaleTimeOffsetX;
        float m_Bubble3ScaleTimeOffsetY;
        Vector3 m_Bubble3DeactivationScale;
        float m_Bubble3DeactivationAlpha;

        enum State
        {
            Activating,
            Deactivating
        }
        State m_State = State.Activating;

        float m_ActivateTime;
        float m_DeactivateTime;

        public void Activate()
        {
            gameObject.SetActive(true);

            m_Bubble1ScaleSpeedX = Random.Range(k_ScaleMinSpeed, k_ScaleMaxSpeed);
            m_Bubble1ScaleSpeedY = Random.Range(k_ScaleMinSpeed, k_ScaleMaxSpeed);
            m_Bubble1ScaleTimeOffsetX = Random.Range(0.0f, 2 * Mathf.PI);
            m_Bubble1ScaleTimeOffsetY = Random.Range(0.0f, 2 * Mathf.PI);

            m_Bubble2ScaleSpeedX = Random.Range(k_ScaleMinSpeed, k_ScaleMaxSpeed);
            m_Bubble2ScaleSpeedY = Random.Range(k_ScaleMinSpeed, k_ScaleMaxSpeed);
            m_Bubble2ScaleTimeOffsetX = Random.Range(0.0f, 2 * Mathf.PI);
            m_Bubble2ScaleTimeOffsetY = Random.Range(0.0f, 2 * Mathf.PI);

            m_Bubble3ScaleSpeedX = Random.Range(k_ScaleMinSpeed, k_ScaleMaxSpeed);
            m_Bubble3ScaleSpeedY = Random.Range(k_ScaleMinSpeed, k_ScaleMaxSpeed);
            m_Bubble3ScaleTimeOffsetX = Random.Range(0.0f, 2 * Mathf.PI);
            m_Bubble3ScaleTimeOffsetY = Random.Range(0.0f, 2 * Mathf.PI);

            m_State = State.Activating;

            m_ActivateTime = 0.0f;

            Update();
        }

        public void Deactivate()
        {
            if (m_State == State.Activating)
            {
                m_State = State.Deactivating;

                m_DeactivateTime = 0.0f;
            }
        }

        void Awake()
        {
            gameObject.SetActive(false);
        }

        void Update()
        {
            m_ActivateTime += Time.deltaTime;
            m_DeactivateTime += Time.deltaTime;

            UpdateBubble(
                m_Bubble1,
                m_Bubble1ScaleSpeedX,
                m_Bubble1ScaleSpeedY,
                m_Bubble1ScaleTimeOffsetX,
                m_Bubble1ScaleTimeOffsetY,
                ref m_Bubble1DeactivationScale,
                ref m_Bubble1DeactivationAlpha,
                0.0f);

            UpdateBubble(
                m_Bubble2,
                m_Bubble2ScaleSpeedX,
                m_Bubble2ScaleSpeedY,
                m_Bubble2ScaleTimeOffsetX,
                m_Bubble2ScaleTimeOffsetY,
                ref m_Bubble2DeactivationScale,
                ref m_Bubble2DeactivationAlpha,
                k_BubbleDelay);

            UpdateBubble(
                m_Bubble3,
                m_Bubble3ScaleSpeedX,
                m_Bubble3ScaleSpeedY,
                m_Bubble3ScaleTimeOffsetX,
                m_Bubble3ScaleTimeOffsetY,
                ref m_Bubble3DeactivationScale,
                ref m_Bubble3DeactivationAlpha,
                k_BubbleDelay * 2,
                m_Text,
                m_Puff);

            if (m_State == State.Deactivating && m_DeactivateTime >= DeactivationDuration)
            {
                gameObject.SetActive(false);
            }
        }

        void UpdateBubble(GameObject bubble, float scaleXSpeed, float scaleYSpeed, float scaleXTimeOffset, float scaleYTimeOffset, ref Vector3 deactivationScale, ref float deactivationAlpha, float delay, TextMeshProUGUI text = null, ParticleSystem puff = null)
        {
            if (m_State == State.Activating || (m_State == State.Deactivating && m_DeactivateTime < delay))
            {
                var scale = new Vector3(m_ActivateScale.Evaluate(m_ActivateTime - delay), m_ActivateScale.Evaluate(m_ActivateTime - delay), 1.0f);
                bubble.transform.localScale = scale;
                deactivationScale = scale;

                var alpha = m_ActivateAlpha.Evaluate(m_ActivateTime - delay);
                bubble.GetComponent<CanvasGroup>().alpha = alpha;
                deactivationAlpha = alpha;
            }

            if (m_State == State.Deactivating && m_DeactivateTime >= delay)
            {
                bubble.transform.localScale = new Vector3(deactivationScale.x * m_DeactivateScale.Evaluate(m_DeactivateTime - delay), deactivationScale.y * m_DeactivateScale.Evaluate(m_DeactivateTime - delay), 1.0f);

                bubble.GetComponent<CanvasGroup>().alpha = deactivationAlpha * m_DeactivateAlpha.Evaluate(m_DeactivateTime - delay);

                if (puff && !puff.isPlaying)
                {
                    puff.Play();
                }
            }

            bubble.transform.localScale = new Vector3(
                bubble.transform.localScale.x * (1.0f + Mathf.Sin((Time.time + scaleXTimeOffset) * scaleXSpeed) * k_ScaleAmplitude),
                bubble.transform.localScale.y * (1.0f + Mathf.Sin((Time.time + scaleYTimeOffset) * scaleYSpeed) * k_ScaleAmplitude),
                1.0f);
        }
    }
}
