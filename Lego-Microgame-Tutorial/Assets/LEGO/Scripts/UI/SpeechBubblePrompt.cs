using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.LEGO.UI.SpeechBubbles;

namespace Unity.LEGO.UI
{
    public class SpeechBubblePrompt : MonoBehaviour
    {
        [Header("Speech Bubble Prefabs")]

        [SerializeField]
        GameObject m_TalkPrefab = default;
        [SerializeField]
        GameObject m_YellPrefab = default;
        [SerializeField]
        GameObject m_ThinkPrefab = default;
        [SerializeField]
        GameObject m_InformationSignPrefab = default;

        public enum Type
        {
            Talk,
            Yell,
            Think,
            InformationSign
        }

        [Serializable]
        public class BubbleInfo
        {
            [Tooltip("The type of the speech bubble.")]
            public Type Type;

            [TextArea, Tooltip("The text shown in the speech bubble.")]
            public string Text;
        }

        public class SpeechInfo
        {
            public List<BubbleInfo> SpeechBubbleInfos;

            public int Id;
            public float Pause;
            public bool Repeat;
            public bool Active;
        }

        public Action<int> OnSpeechFinished;

        List<SpeechInfo> m_Speeches = new List<SpeechInfo>();
        int m_NextSpeechId;
        SpeechInfo m_CurrentSpeech;

        const float k_WaitTimeBeforeClosingBubble = 1.5f;
        const float k_WaitTimeBetweenBubbles = 0.2f;
        const float k_MinWaitTimePerCharacter = 0.08f;
        const float k_MaxWaitTimePerCharacter = 0.12f;
        const float k_ExtendedWriteDelay = 0.5f; // Extended delay is used for writing characters specified in m_ExtendedDelayCharacters.

        const string k_TextAlpha = "<alpha=#00>";

        static readonly char[] m_ExtendedDelayCharacters =
        {
            '.', ',', '?', '!', ':', ';'
        };

        static readonly Type[] m_TypesToInstantWrite =
        {
            Type.Yell, Type.InformationSign
        };

        Dictionary<Type, GameObject> m_SpeechBubbles = new Dictionary<Type, GameObject>();
        ISpeechBubble m_CurrentSpeechBubble;

        Dictionary<int, float> m_FadeValues = new Dictionary<int, float>();

        char[] m_CurrentCharacters;
        string m_CurrentText;
        bool m_WritingText;
        int m_TextCharacterIndex;
        int m_CurrentSpeechTextIndex;
        float m_WriteTimer;

        float m_WaitTime;
        float m_AlternateTime;
        float m_AlternateDelay;

        bool m_IsWaiting;
        bool m_IsTextInstant;
        bool m_IsTextFading;
        bool m_PauseActive;

        PromptPlacementHandler m_PromptHandler;

        public int AddSpeech(List<BubbleInfo> speechBubbleInfos, float pause, bool repeat, Action<int> SpeechFinished, bool activeFromStart, PromptPlacementHandler promptHandler)
        {
            m_PromptHandler = promptHandler;

            m_Speeches.Add(new SpeechInfo()
            {
                SpeechBubbleInfos = speechBubbleInfos,
                Id = m_NextSpeechId,
                Pause = pause,
                Repeat = repeat,
                Active = activeFromStart
            });

            OnSpeechFinished += SpeechFinished;

            if (activeFromStart && GetActiveSpeechCount() == 1)
            {
                Activate(m_NextSpeechId);
            }

            return m_NextSpeechId++;
        }

        public void Activate(int id)
        {
            var speechInfo = m_Speeches.FirstOrDefault(entry => entry.Id == id);
            if (speechInfo != null)
            {
                speechInfo.Active = true;

                if (GetActiveSpeechCount() == 1)
                {
                    m_CurrentSpeech = speechInfo;
                    m_CurrentSpeechTextIndex = 0;

                    StartActivate();
                    m_PromptHandler.Activate(PromptPlacementHandler.PromptType.SpeechBubble);
                }
            }
        }

        public void Deactivate(int id)
        {
            var speechInfo = m_Speeches.FirstOrDefault(entry => entry.Id == id);
            if (speechInfo != null)
            {
                // If only this id is active, deactivate the speech bubble.
                if (GetActiveSpeechCount() == 1 && speechInfo.Active)
                {
                    StartDeactivate();
                    m_PromptHandler.Deactivate(PromptPlacementHandler.PromptType.SpeechBubble);
                }

                speechInfo.Active = false;
            }
        }

        void Awake()
        {
            m_SpeechBubbles.Add(Type.Talk, Instantiate(m_TalkPrefab, transform.position + m_TalkPrefab.transform.position, transform.rotation, transform));
            m_SpeechBubbles.Add(Type.Yell, Instantiate(m_YellPrefab, transform.position + m_YellPrefab.transform.position, transform.rotation, transform));
            m_SpeechBubbles.Add(Type.Think, Instantiate(m_ThinkPrefab, transform.position + m_ThinkPrefab.transform.position, transform.rotation, transform));
            m_SpeechBubbles.Add(Type.InformationSign, Instantiate(m_InformationSignPrefab, transform.position + m_InformationSignPrefab.transform.position, transform.rotation, transform));
        }

        void Update()
        {
            // Wait.
            if (m_IsWaiting)
            {
                m_WaitTime -= Time.deltaTime;

                if (m_WaitTime < 0.0f)
                {
                    m_IsWaiting = false;
                }
            }

            if (GetActiveSpeechCount() > 0 && !m_IsWaiting)
            {
                if (m_WritingText)
                {
                    SpeechWriter();
                }
                else
                {
                    // Alternate to next bubble after a pause.
                    var time = m_AlternateTime;
                    var waitDuration = m_IsTextInstant ? k_WaitTimeBeforeClosingBubble + m_AlternateDelay : k_WaitTimeBeforeClosingBubble;
                    m_AlternateTime += Time.deltaTime;

                    if (m_AlternateTime >= waitDuration)
                    {
                        if (time <= waitDuration && !m_PauseActive)
                        {
                            if (m_CurrentSpeechTextIndex == m_CurrentSpeech.SpeechBubbleInfos.Count - 1)
                            {
                                m_AlternateTime -= m_CurrentSpeech.Pause;
                                m_PauseActive = true;
                            }

                            // Deactivate bubble.
                            StartDeactivate();
                        }
                        else if (m_AlternateTime >= waitDuration + m_CurrentSpeechBubble.DeactivationDuration + k_WaitTimeBetweenBubbles)
                        {
                            // Set next speech bubble.
                            m_AlternateTime = 0.0f;
                            m_PauseActive = false;

                            NextBubble();
                        }
                    }
                }
            }

            // Rotate object to look at main camera.
            transform.rotation = Quaternion.LookRotation(-Camera.main.transform.forward, Camera.main.transform.up);
        }

        void StartActivate()
        {
            SetupBubble();

            m_IsWaiting = true;
            m_WaitTime = m_CurrentSpeechBubble.TextDelay;

            // Start the activate animation.
            m_CurrentSpeechBubble.Activate();
        }

        void StartDeactivate()
        {
            // Start the deactivate animation.
            m_CurrentSpeechBubble.Deactivate();
        }

        void NextBubble()
        {
            m_CurrentSpeechTextIndex++;

            // Check if we are done with the current speech.
            if (m_CurrentSpeechTextIndex == m_CurrentSpeech.SpeechBubbleInfos.Count)
            {
                if (!m_CurrentSpeech.Repeat)
                {
                    OnSpeechFinished?.Invoke(m_CurrentSpeech.Id);
                }

                m_CurrentSpeechTextIndex = 0;

                // Find the next active speech.
                if (GetActiveSpeechCount() > 0)
                {
                    // Find current speech index. The speech can have been removed.
                    var currentIndex = 0;

                    for (var i = 0; i < m_Speeches.Count; i++)
                    {
                        if (m_CurrentSpeech == m_Speeches.ElementAt(i))
                        {
                            currentIndex = i;
                            break;
                        }
                    }

                    // Get next speech index and stop when an active speech has been found.
                    // We know this loop will terminate as we have at least one active speech.
                    do
                    {
                        currentIndex = (currentIndex + 1) % m_Speeches.Count;
                    }
                    while (!m_Speeches.ElementAt(currentIndex).Active);

                    m_CurrentSpeech = m_Speeches[currentIndex];
                }
            }

            // Activate the new bubble. 
            if (GetActiveSpeechCount() > 0)
            {
                StartActivate();
            }
        }

        void SetupBubble()
        {
            var speechBubbleInfo = m_CurrentSpeech.SpeechBubbleInfos[m_CurrentSpeechTextIndex];
            m_CurrentSpeechBubble = m_SpeechBubbles[speechBubbleInfo.Type].GetComponent<ISpeechBubble>();
            m_PromptHandler.SetHeight(PromptPlacementHandler.PromptType.SpeechBubble, m_CurrentSpeechBubble.Height);

            SetupWriter(speechBubbleInfo);
        }

        void SetupWriter(BubbleInfo info)
        {
            m_CurrentCharacters = info.Text.ToCharArray();
            m_CurrentText = info.Text;
            m_AlternateDelay = 0.0f;
            m_WriteTimer = 0.0f;
            m_TextCharacterIndex = 0;

            m_FadeValues.Clear();

            m_IsTextInstant = m_TypesToInstantWrite.Contains(info.Type);
            m_IsTextFading = info.Type == Type.Think;
            m_WritingText = !m_IsTextInstant && m_CurrentText.Length > 0;

            if (m_IsTextInstant)
            {
                m_AlternateDelay = k_MinWaitTimePerCharacter * m_CurrentCharacters.Length;
                m_CurrentSpeechBubble.Text.text = m_CurrentText;
            }
            else
            {
                // Add rich text alpha tag for text to start invisible.
                m_CurrentSpeechBubble.Text.text = k_TextAlpha + m_CurrentText;
            }
        }

        void SpeechWriter()
        {
            m_WriteTimer += Time.deltaTime;

            if (m_WriteTimer >= m_AlternateDelay)
            {
                if (m_TextCharacterIndex < m_CurrentCharacters.Length)
                {
                    if (m_IsTextFading)
                    {
                        var index = m_TextCharacterIndex * k_TextAlpha.Length + m_TextCharacterIndex;
                        m_FadeValues.Add(index, 0.0f);
                    }
                    else
                    {
                        var tempText = m_CurrentText;
                        tempText = tempText.Insert(m_TextCharacterIndex + 1, k_TextAlpha);

                        m_CurrentSpeechBubble.Text.text = tempText;
                    }

                    // Set delay for next character.
                    if (m_ExtendedDelayCharacters.Contains(m_CurrentCharacters[m_TextCharacterIndex]))
                    {
                        m_AlternateDelay += k_ExtendedWriteDelay;
                    }
                    else
                    {
                        m_AlternateDelay += UnityEngine.Random.Range(k_MinWaitTimePerCharacter, k_MaxWaitTimePerCharacter);
                    }

                    m_TextCharacterIndex++;
                }
                else if (!m_IsTextFading)
                {
                    m_WritingText = false;
                }
            }

            if (m_IsTextFading)
            {
                SpeechFader();
            }
        }

        void SpeechFader()
        {
            if (m_FadeValues.Count > 0)
            {
                var tempText = m_CurrentText;
                var fadeValue = 0;

                for (var i = 0; i < m_FadeValues.Count; i++)
                {
                    var index = m_FadeValues.Keys.ElementAt(i);

                    m_FadeValues[index] += 255 * Time.deltaTime;
                    fadeValue = (int)m_FadeValues[index];

                    var alphaValue = fadeValue >= 255 ? "<alpha=#FF>" : "<alpha=#" + fadeValue.ToString("X2") + ">";

                    tempText = tempText.Insert(index, alphaValue);
                }

                // Insert alpha of 0 for the rest of the characters not written yet.
                tempText = tempText.Insert(m_TextCharacterIndex * k_TextAlpha.Length + m_TextCharacterIndex, "<alpha=#00>");

                // Set text.
                m_CurrentSpeechBubble.Text.text = tempText;

                if (m_FadeValues.Count == m_CurrentCharacters.Length && fadeValue >= 255)
                {
                    m_WritingText = false;
                }
            }
        }

        int GetActiveSpeechCount()
        {
            return m_Speeches.Count(entry => entry.Active);
        }
    }
}
