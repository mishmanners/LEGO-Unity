using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.LEGO.UI
{
    public class InputPrompt : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Tooltip("The text field displaying the input.")]
        TMPro.TextMeshProUGUI m_Text = default;

        [SerializeField, Tooltip("The main canvas displaying the input prompt.")]
        RectTransform m_CanvasTransform = default;

        [Header("Movement")]

        [SerializeField, Tooltip("The animation curve for scaling when activated.")]
        AnimationCurve m_ActivateCurve = default;
        [SerializeField, Tooltip("The animation curve for scaling when deactivated.")]
        AnimationCurve m_DeactivateCurve = default;
        [SerializeField, Tooltip("The animation curve for scaling when receiving input.")]
        AnimationCurve m_InputCurve = default;
        [SerializeField, Tooltip("The animation curve for rotation when showing the next available input.")]
        AnimationCurve m_AlternateCurve = default;

        [SerializeField, Tooltip("The delay before rotating to show the next available input.")]
        float m_AlternateDelay = 2.0f;

        class LabelInfo
        {
            public bool Active;
            public List<int> Distances;
        }

        float m_ScaleTime;
        AnimationCurve m_CurrentScaleCurve;
        float m_CurrentScaleSpan;
        bool m_DeactivateAfterCurrentScale;

        float m_AlternateTime;
        float m_LabelChangeDelay;
        bool m_LabelChanged;

        float m_ActivateSpan;
        float m_DeactivateSpan;
        float m_InputSpan;
        float m_AlternateSpan;

        readonly Dictionary<string, LabelInfo> m_LabelDictionary = new Dictionary<string, LabelInfo>();

        PromptPlacementHandler m_PromptHandler;

        public void AddLabel(string label, bool activeFromStart, int distance, PromptPlacementHandler promptHandler)
        {
            m_PromptHandler = promptHandler;
            m_PromptHandler.SetHeight(PromptPlacementHandler.PromptType.InputPrompt, m_CanvasTransform.sizeDelta.y);

            if (m_LabelDictionary.ContainsKey(label))
            {
                var labelInfo = m_LabelDictionary[label];
                labelInfo.Active |= activeFromStart;
                labelInfo.Distances.Add(distance);
            }
            else
            {
                m_LabelDictionary.Add(label, new LabelInfo()
                {
                    Active = activeFromStart,
                    Distances = new List<int> { distance }
                });
            }

            if (activeFromStart)
            {
                m_Text.text = label;
            }

            m_CanvasTransform.localScale = GetActiveLabelsCount() > 0 ? Vector3.one : Vector3.zero;
        }


        public void Activate(string label)
        {
            if (m_LabelDictionary.ContainsKey(label))
            {
                // If no label is currently active, activate the prompt.
                if (GetActiveLabelsCount() == 0)
                {
                    m_Text.text = label;

                    StartScale(m_ActivateCurve, m_ActivateSpan);
                    m_PromptHandler.Activate(PromptPlacementHandler.PromptType.InputPrompt);
                }

                m_LabelDictionary[label].Active = true;
            }
        }

        public void Deactivate(string label, int distance)
        {
            if (m_LabelDictionary.ContainsKey(label))
            {
                // If it is the largest distance, the label should be deactivated.
                if (GetLargestDistance(label) == distance)
                {
                    // If only this label is active, deactivate the prompt.
                    if (GetActiveLabelsCount() == 1 && m_LabelDictionary[label].Active)
                    {
                        StartScale(m_DeactivateCurve, m_DeactivateSpan);
                        m_PromptHandler.Deactivate(PromptPlacementHandler.PromptType.InputPrompt);
                    }

                    m_LabelDictionary[label].Active = false;
                }
            }
        }

        public void Input(string label, int distance, bool repeatable, bool visible)
        {
            if (m_LabelDictionary.ContainsKey(label))
            {
                var deactivateAfterInput = false;

                if (!repeatable)
                {
                    var largestDistanceBeforeRemoval = GetLargestDistance(label);
                    m_LabelDictionary[label].Distances.Remove(distance);

                    // If we removed a largest distance, we need to check if the label should be deactivated.
                    if (largestDistanceBeforeRemoval == distance)
                    {
                        // Check if there are any distances left.
                        if (m_LabelDictionary[label].Distances.Count > 0)
                        {
                            // If there are, check if the largest left is smaller than the distance.
                            if (GetLargestDistance(label) < distance)
                            {
                                m_LabelDictionary[label].Active = false;

                                // If we are showing the label, skip the alternate delay.
                                if (m_Text.text == label && GetActiveLabelsCount() > 0)
                                {
                                    m_AlternateTime = Mathf.Max(m_AlternateTime, m_AlternateDelay);
                                }
                            }
                        }
                        else
                        {
                            // If there aren't we will remove the label.
                            m_LabelDictionary.Remove(label);

                            // If we are showing the label, skip the alternate delay.
                            if (m_Text.text == label && GetActiveLabelsCount() > 0)
                            {
                                m_AlternateTime = Mathf.Max(m_AlternateTime, m_AlternateDelay);
                            }
                        }

                        // If there are no active labels left, we should deactivate the prompt.
                        if (GetActiveLabelsCount() == 0)
                        {
                            deactivateAfterInput = true;
                        }
                    }
                }

                // If the Input Trigger is visible, show input animation.
                if (visible)
                {
                    StartScale(m_InputCurve, m_InputSpan, deactivateAfterInput);
                }
            }
        }

        void OnValidate()
        {
            m_AlternateDelay = Mathf.Max(0.5f, m_AlternateDelay);
        }

        void Awake()
        {
            m_ActivateSpan = m_ActivateCurve.keys[m_ActivateCurve.keys.Length - 1].time;
            m_DeactivateSpan = m_DeactivateCurve.keys[m_DeactivateCurve.keys.Length - 1].time;
            m_InputSpan = m_InputCurve.keys[m_InputCurve.keys.Length - 1].time;
            m_AlternateSpan = m_AlternateCurve.keys[m_AlternateCurve.keys.Length - 1].time;
            m_LabelChangeDelay = m_AlternateSpan * 4.0f / 7.0f;
        }

        void Update()
        {
            // Alternate.
            var activeLabelsCount = GetActiveLabelsCount();
            var alternate = activeLabelsCount > 1; // We should alternate, if more than one label is active.
            alternate |= activeLabelsCount == 1 && m_Text.text != GetFirstActiveLabel(); // OR if one label is active and we are not showing it.
            alternate |= m_AlternateTime >= m_AlternateDelay; // OR if we are already rotating.
            if (alternate)
            {
                m_AlternateTime += Time.deltaTime;

                if (m_AlternateTime >= m_AlternateDelay)
                {
                    m_CanvasTransform.localRotation = Quaternion.Euler(Vector3.up * ((m_AlternateCurve.Evaluate(m_AlternateTime - m_AlternateDelay) * 180.0f) + 180.0f));

                    if (m_AlternateTime >= m_AlternateDelay + m_LabelChangeDelay && !m_LabelChanged)
                    {
                        m_Text.text = GetNextActiveLabel();

                        m_LabelChanged = true;
                    }

                    if (m_AlternateTime >= m_AlternateDelay + m_AlternateSpan)
                    {
                        m_AlternateTime -= m_AlternateDelay + m_AlternateSpan;
                        m_LabelChanged = false;
                    }
                }
            }

            // Scale.
            if (m_CurrentScaleCurve != null)
            {
                m_ScaleTime += Time.deltaTime;
                m_CanvasTransform.localScale = Vector3.one * m_CurrentScaleCurve.Evaluate(m_ScaleTime);

                if (m_ScaleTime >= m_CurrentScaleSpan)
                {
                    if (m_DeactivateAfterCurrentScale)
                    {
                        StartScale(m_DeactivateCurve, m_DeactivateSpan);
                    }
                    else
                    {
                        m_CurrentScaleCurve = null;
                    }
                }
            }

            // Rotate object to look at main camera.
            transform.rotation = Quaternion.LookRotation(-Camera.main.transform.forward, Camera.main.transform.up);
        }

        void StartScale(AnimationCurve scaleCurve, float span, bool deactivateAfter = false)
        {
            m_CurrentScaleCurve = scaleCurve;
            m_CurrentScaleSpan = span;
            m_ScaleTime = 0.0f;
            m_DeactivateAfterCurrentScale = deactivateAfter;
        }

        string GetNextActiveLabel()
        {
            // If no labels are active, just keep the current label.
            if (GetActiveLabelsCount() == 0)
            {
                return m_Text.text;
            }

            // Find current label index. The label can have been removed.
            var currentLabelIndex = 0;

            for (int i = 0; i < m_LabelDictionary.Count; i++)
            {
                if (m_Text.text == m_LabelDictionary.ElementAt(i).Key)
                {
                    currentLabelIndex = i;
                    break;
                }
            }

            // Get next label index and stop when an active label has been found.
            // We know this loop will terminate as we have at least one active label.
            do
            {
                currentLabelIndex = (currentLabelIndex + 1) % m_LabelDictionary.Count;
            }
            while (!m_LabelDictionary.ElementAt(currentLabelIndex).Value.Active);

            return m_LabelDictionary.ElementAt(currentLabelIndex).Key;
        }

        int GetActiveLabelsCount()
        {
            return m_LabelDictionary.Count(entry => entry.Value.Active);
        }

        string GetFirstActiveLabel()
        {
            return m_LabelDictionary.First(entry => entry.Value.Active).Key;
        }

        int GetLargestDistance(string label)
        {
            return m_LabelDictionary[label].Distances.Max();
        }
    }
}
