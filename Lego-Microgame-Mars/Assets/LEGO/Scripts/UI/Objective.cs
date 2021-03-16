using UnityEngine;
using UnityEngine.UI;
using Unity.LEGO.Game;

namespace Unity.LEGO.UI
{
    // This is the component that is responsible for the Objectives display in the top left corner of the screen

    [RequireComponent(typeof(RectTransform))]
    public class Objective : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Tooltip("The text that will display the title.")]
        TMPro.TextMeshProUGUI m_TitleText = default;

        [SerializeField, Tooltip("The text that will display the description.")]
        TMPro.TextMeshProUGUI m_DescriptionText = default;

        [SerializeField, Tooltip("The text that will display the progress.")]
        TMPro.TextMeshProUGUI m_ProgressText = default;

        [SerializeField, Tooltip("The icon that shows when objective is complete.")]
        Image m_CompleteIcon = default;

        [Header("Movement")]

        [SerializeField, Tooltip("The animation curve for moving in.")]
        AnimationCurve m_MoveInCurve = default;

        float m_Time;
        const float s_Margin = 25;
        const float s_TitleAndProgressSpacing = 4;

        RectTransform m_RectTransform;

        public void Initialize(string title, string description, string progress)
        {
            m_RectTransform = GetComponent<RectTransform>();

            // Set progress text.
            m_ProgressText.text = progress;
            m_ProgressText.ForceMeshUpdate();

            // Set title text and margin to make room for progress text.
            Vector4 margin = m_TitleText.margin;
            margin.z = 4 + (string.IsNullOrEmpty(progress) ? 0 : m_ProgressText.renderedWidth + s_TitleAndProgressSpacing);
            m_TitleText.margin = margin;
            m_TitleText.text = title;

            // Set description text.
            m_DescriptionText.text = description;
        }

        public void UpdateProgress(string progress)
        {
            m_ProgressText.text = progress;
        }

        public void OnProgress(IObjective objective)
        {
            m_ProgressText.text = objective.GetProgress();

            if (objective.IsCompleted)
            {
                m_CompleteIcon.gameObject.SetActive(true);
            }
        }

        void Update()
        {
            // Update time.
            m_Time += Time.deltaTime;

            // Move in.
            m_RectTransform.anchoredPosition = new Vector2((m_RectTransform.sizeDelta.x + s_Margin) * m_MoveInCurve.Evaluate(m_Time), m_RectTransform.anchoredPosition.y);
        }
    }
}
