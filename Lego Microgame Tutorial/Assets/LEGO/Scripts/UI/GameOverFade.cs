using Unity.LEGO.Game;
using UnityEngine;

namespace Unity.LEGO.UI
{
    public class GameOverFade : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Tooltip("The canvas group that contains the fade image.")]
        CanvasGroup m_CanvasGroup = default;

        [Header("Fade")]

        [SerializeField, Tooltip("The delay in seconds before fading starts when winning.")]
        float m_WinDelay = 4.0f;

        [SerializeField, Tooltip("The delay in seconds before fading starts when losing.")]
        float m_LoseDelay = 2.0f;

        [SerializeField, Tooltip("The duration in seconds of the fade.")]
        float m_Duration = 1.0f;

        float m_Time;
        bool m_GameOver;
        bool m_Won;

        void Start()
        {
            EventManager.AddListener<GameOverEvent>(OnGameOver);    
        }

        void Update()
        {
            if (m_GameOver)
            {
                // Update time.
                m_Time += Time.deltaTime;

                // Fade.
                if (m_Won)
                    m_CanvasGroup.alpha = Mathf.Clamp01((m_Time - m_WinDelay) / m_Duration);
                else
                    m_CanvasGroup.alpha = Mathf.Clamp01((m_Time - m_LoseDelay) / m_Duration);
            }
        }

        void OnGameOver(GameOverEvent evt)
        {
            if (!m_GameOver)
            {
                m_CanvasGroup.gameObject.SetActive(true);
                m_GameOver = true;
                m_Won = evt.Win;
            }
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<GameOverEvent>(OnGameOver);
        }
    }
}
