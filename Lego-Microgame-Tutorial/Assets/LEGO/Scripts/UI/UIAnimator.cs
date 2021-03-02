using UnityEngine;

namespace Unity.LEGO.UI
{
    public class UIAnimator : MonoBehaviour
    {
        [Header("References")]
        
        [SerializeField, Tooltip("The canvas group used for transparency.")]
        CanvasGroup m_CanvasGroup = default;

        [SerializeField, Tooltip("The transform rect used for scaling.")]
        RectTransform m_RectTransform = default;

        [Header("Animation")]

        [SerializeField, Tooltip("The delay in seconds before the effect.")]
        float m_Delay = 0.0f;

        [SerializeField, Tooltip("The animation curve for scaling.")]
        AnimationCurve m_ScaleCurve = default;

        [SerializeField, Tooltip("The animation curve for transparency.")]
        AnimationCurve m_AlphaCurve = default;

        float m_Time;
        
        void Update()
        {
            // Update time.
            m_Time += Time.deltaTime;

            // Set transparency.
            m_CanvasGroup.alpha = m_AlphaCurve.Evaluate(m_Time - m_Delay);

            // Set scale.
            var scale = m_ScaleCurve.Evaluate(m_Time - m_Delay);
            m_RectTransform.localScale = new Vector3(scale, scale, 1.0f);
        }
    }
}
