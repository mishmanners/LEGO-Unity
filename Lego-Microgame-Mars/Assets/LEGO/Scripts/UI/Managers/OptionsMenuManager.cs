using Unity.LEGO.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Unity.LEGO.UI
{
    // This is the component that handles the the InGameMenu.
    // Press TAB at runtime to show the menu.

    public class OptionsMenuManager : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Tooltip("The canvas that holds the menu.")]
        GameObject m_Menu = default;

        [SerializeField, Tooltip("The toggle component for shadows.")]
        Toggle m_ShadowsToggle = default;

        [SerializeField, Tooltip("The toggle component for frame rate counter.")]
        Toggle m_FrameRateCounterToggle = default;

        [SerializeField, Tooltip("The game object for the controls.")]
        GameObject m_Controls = default;

        [SerializeField, Tooltip("The Look sensitivity")]
        Slider m_LookSensitivity = default;

        FrameRateCounter m_FrameRateCounter;

        void Start()
        {
            m_FrameRateCounter = FindObjectOfType<FrameRateCounter>();

            if (m_FrameRateCounter == null)
                Debug.LogError("FrameRate Counter is missing!");

            m_Menu.SetActive(false);

            m_ShadowsToggle.SetIsOnWithoutNotify(QualitySettings.shadows != ShadowQuality.Disable);
            m_ShadowsToggle.onValueChanged.AddListener(OnShadowsChanged);

            m_FrameRateCounterToggle.SetIsOnWithoutNotify(m_FrameRateCounter.IsShowing);
            m_FrameRateCounterToggle.onValueChanged.AddListener(OnFramerateCounterChanged);

            var defaultValue = PlayerPrefs.GetFloat("LookSensitivity", 5.0f);
            m_LookSensitivity.SetValueWithoutNotify(defaultValue);
            m_LookSensitivity.onValueChanged.AddListener(OnLookSensitivityChanged);
            OnLookSensitivityChanged(defaultValue);
        }

        void OnLookSensitivityChanged(float value)
        {
            PlayerPrefs.SetFloat("LookSensitivity", value);
            PlayerPrefs.Save();

            LookSensitivityUpdateEvent lookSensitivityUpdateEvent = Events.LookSensitivityUpdateEvent;
            lookSensitivityUpdateEvent.Value = value;
            EventManager.Broadcast(lookSensitivityUpdateEvent);
        }

        public void ClosePauseMenu()
        {
            SetPauseMenuActivation(false);
        }

        public void TogglePauseMenu()
        {
            SetPauseMenuActivation(!(m_Menu.activeSelf || m_Controls.activeSelf));
        }

        void Update()
        {
            if (Input.GetButtonDown("InGameMenuOption"))
            {
                TogglePauseMenu();
            }
        }

        void SetPauseMenuActivation(bool active)
        {
#if !UNITY_EDITOR
            Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
#endif

            m_Menu.SetActive(active);
            m_Controls.SetActive(false);

            if (m_Menu.activeSelf)
            {
                Time.timeScale = 0f;

                EventSystem.current.SetSelectedGameObject(null);
            }
            else
            {
                Time.timeScale = 1f;
            }

            OptionsMenuEvent evt = Events.OptionsMenuEvent;
            evt.Active = active;
            EventManager.Broadcast(evt);
        }

        void OnShadowsChanged(bool newValue)
        {
            QualitySettings.shadows = newValue ? ShadowQuality.All : ShadowQuality.Disable;
        }

        void OnFramerateCounterChanged(bool newValue)
        {
            m_FrameRateCounter.Show(newValue);
        }
    }
}
