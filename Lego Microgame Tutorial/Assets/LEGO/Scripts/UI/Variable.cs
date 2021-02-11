using UnityEngine;

namespace Unity.LEGO.UI
{
    // This is the component that is responsible for the Variable display in the top right corner of the screen

    [RequireComponent(typeof(RectTransform))]
    public class Variable : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Tooltip("The text that will display the name.")]
        TMPro.TextMeshProUGUI m_NameText = default;

        [SerializeField, Tooltip("The text that will display the value.")]
        TMPro.TextMeshProUGUI m_ValueText = default;

        [Header("Movement")]

        [SerializeField, Tooltip("The animation curve for moving in.")]
        AnimationCurve m_MoveInCurve = default;

        float m_Time;
        const float s_Margin = 15;
        const float s_NameAndValueSpacing = 4;

        RectTransform m_RectTransform;

        public void Initialize(string title, string progress)
        {
            m_RectTransform = GetComponent<RectTransform>();

            // Set value text.
            m_ValueText.text = progress;
            m_ValueText.ForceMeshUpdate();

            // Set name text and margin to make room for value text.
            Vector4 margin = m_NameText.margin;
            margin.z = 4 + (string.IsNullOrEmpty(progress) ? 0 : m_ValueText.renderedWidth + s_NameAndValueSpacing);
            m_NameText.margin = margin;
            m_NameText.text = title;
        }

        public void OnUpdate(int value)
        {
            m_ValueText.text = value.ToString();
        }

        void Update()
        {
            // Update time.
            m_Time += Time.deltaTime;

            // Move in.
            m_RectTransform.anchoredPosition = new Vector2((m_RectTransform.sizeDelta.x + s_Margin) * -m_MoveInCurve.Evaluate(m_Time), m_RectTransform.anchoredPosition.y);
        }
    }
}
