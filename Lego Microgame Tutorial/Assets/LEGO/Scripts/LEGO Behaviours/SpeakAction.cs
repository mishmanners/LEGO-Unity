using System.Collections.Generic;
using Unity.LEGO.Behaviours.Actions;
using Unity.LEGO.UI;
using UnityEngine;

namespace Unity.LEGO.Behaviours
{
    public class SpeakAction : RepeatableAction
    {
        public const int MaxCharactersPerSpeechBubble = 60;

        [SerializeField]
        List<SpeechBubblePrompt.BubbleInfo> m_SpeechBubbleInfos = new List<SpeechBubblePrompt.BubbleInfo>
            { new SpeechBubblePrompt.BubbleInfo { Text = "Hello!", Type = SpeechBubblePrompt.Type.Talk } };

        [SerializeField]
        GameObject m_SpeechBubblePromptPrefab = default;

        SpeechBubblePrompt m_SpeechBubblePrompt;
        bool m_PromptActive = true;
        int m_Id;

        protected override void OnValidate()
        {
            base.OnValidate();

            m_Pause = Mathf.Max(0.0f, m_Pause);
        }

        protected override void Reset()
        {
            base.Reset();

            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Speak Action.png";
        }

        protected override void Start()
        {
            base.Start();

            if (IsPlacedOnBrick())
            {
                foreach (var speechBubleInfo in m_SpeechBubbleInfos)
                {
                    if (speechBubleInfo.Text.Length > MaxCharactersPerSpeechBubble)
                    {
                        speechBubleInfo.Text = speechBubleInfo.Text.Substring(0, MaxCharactersPerSpeechBubble);
                    }
                }
            }
        }

        protected void Update()
        {
            if (m_Active)
            {
                if (m_SpeechBubbleInfos.Count > 0 && !m_SpeechBubblePrompt)
                {
                    SetupPrompt();
                }

                UpdatePrompt(IsVisible());
            }
        }

        void SetupPrompt()
        {
            PromptPlacementHandler promptHandler = null;

            foreach (var brick in m_ScopedBricks)
            {
                if (brick.GetComponent<PromptPlacementHandler>())
                {
                    promptHandler = brick.GetComponent<PromptPlacementHandler>();
                }

                var speakActions = brick.GetComponents<SpeakAction>();

                foreach (var speakAction in speakActions)
                {
                    if (speakAction.m_SpeechBubblePrompt)
                    {
                        m_SpeechBubblePrompt = speakAction.m_SpeechBubblePrompt;
                        break;
                    }
                }
            }

            var activeFromStart = IsVisible();

            // Create a new speech bubble prompt if none was found.
            if (!m_SpeechBubblePrompt)
            {
                if (!promptHandler)
                {
                    promptHandler = gameObject.AddComponent<PromptPlacementHandler>();
                }

                var go = Instantiate(m_SpeechBubblePromptPrefab, promptHandler.transform);
                m_SpeechBubblePrompt = go.GetComponent<SpeechBubblePrompt>();

                // Get the current scoped bounds - might be different than the initial scoped bounds.
                var scopedBounds = GetScopedBounds(m_ScopedBricks, out _, out _);
                promptHandler.AddInstance(go, scopedBounds, PromptPlacementHandler.PromptType.SpeechBubble, activeFromStart);
            }

            // Add this Speak Action to the speech bubble prompt.
            m_Id = m_SpeechBubblePrompt.AddSpeech(m_SpeechBubbleInfos, m_Pause, m_Repeat, SpeechFinished, activeFromStart, promptHandler);
        }

        void UpdatePrompt(bool active)
        {
            if (m_PromptActive != active)
            {
                m_PromptActive = active;

                if (active)
                {
                    m_SpeechBubblePrompt.Activate(m_Id);
                }
                else
                {
                    m_SpeechBubblePrompt.Deactivate(m_Id);
                }
            }
        }

        void SpeechFinished(int id)
        {
            if (m_Id == id)
            {
                UpdatePrompt(false);
                m_Active = false;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (m_SpeechBubblePrompt)
            {
                UpdatePrompt(false);
            }
        }
    }
}
