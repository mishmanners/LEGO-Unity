using UnityEngine;
using TMPro;

namespace Unity.LEGO.UI
{
    // A simple component that displays the frame rate at runtime.
    // It is accessible through the options menu.

    public class FrameRateCounter : MonoBehaviour
    {
        public bool IsShowing => m_TextField.gameObject.activeSelf;

        [Header("References")]

        [SerializeField, Tooltip("The text field displaying the frame rate.")]
        TextMeshProUGUI m_TextField = default;

        [Header("Frame Rate")]

        [SerializeField, Tooltip("The delay in seconds between updates of the displayed frame rate.")]
        float m_PollingTime = 0.5f;

        float m_Time;
        int m_FrameCount;

        public void Show(bool show)
        {
            m_TextField.gameObject.SetActive(show);
        }

        void Update()
        {
            // Update time.
            m_Time += Time.deltaTime;

            // Count this frame.
            m_FrameCount++;

            if (m_Time >= m_PollingTime)
            {
                // Update frame rate.
                int frameRate = Mathf.RoundToInt((float)m_FrameCount / m_Time);
                m_TextField.text = frameRate.ToString();

                // Reset time and frame frame count.
                m_Time -= m_PollingTime;
                m_FrameCount = 0;
            }
        }
    }
}
