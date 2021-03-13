using System.Collections.Generic;
using UnityEngine;

namespace Unity.LEGO.UI
{
    public class PromptPlacementHandler : MonoBehaviour
    {
        // The types of prompt and their priority in the order they are listed.
        public enum PromptType
        {
            SpeechBubble,
            InputPrompt
        }

        class PromptInfo
        {
            public GameObject Instance;
            public float Height;
            public bool Active;
        }

        SortedDictionary<PromptType, PromptInfo> m_Prompts = new SortedDictionary<PromptType, PromptInfo>();

        Vector3 m_BasePosition;

        const float k_PromptDistance = 0.6f;
        const float k_PlacementAnimationSpeed = 10.0f;

        bool m_IsFirstFrame = true;

        void Update()
        {
            UpdatePromptPlacements(!m_IsFirstFrame);
            m_IsFirstFrame = false;
        }

        public void AddInstance(GameObject promptInstance, Bounds bounds, PromptType type, bool active)
        {
            m_BasePosition = transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.center.y + bounds.extents.y, bounds.center.z));

            if (!m_Prompts.ContainsKey(type))
            {
                var promptInfo = new PromptInfo {Instance = promptInstance, Active = active};
                m_Prompts.Add(type, promptInfo);
            }
            else
            {
                m_Prompts[type].Active |= active;
            }
        }

        void UpdatePromptPlacements(bool animatePlacement)
        {
            var promptHeight = k_PromptDistance;

            foreach (var promptInfo in m_Prompts.Values)
            {
                if (promptInfo.Active)
                {
                    // Set target position.
                    var targetPosition = transform.TransformPoint(m_BasePosition) + Vector3.up * promptHeight;

                    // Set height for next prompt.
                    promptHeight += k_PromptDistance + promptInfo.Height;

                    if (animatePlacement)
                    {
                        promptInfo.Instance.transform.position = Vector3.Lerp(promptInfo.Instance.transform.position, targetPosition, Time.deltaTime * k_PlacementAnimationSpeed);
                    }
                    else
                    {
                        promptInfo.Instance.transform.position = targetPosition;
                    }
                }
            }
        }

        public void SetHeight(PromptType type, float height)
        {
            if (m_Prompts.ContainsKey(type))
            {
                m_Prompts[type].Height = height;
            }
        }

        public void Activate(PromptType type)
        {
            if (m_Prompts.ContainsKey(type))
            {
                m_Prompts[type].Active = true;
            }
        }

        public void Deactivate(PromptType type)
        {
            if (m_Prompts.ContainsKey(type))
            {
                m_Prompts[type].Active = false;
            }
        }
    }
}
