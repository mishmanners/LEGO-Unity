using Cinemachine;
using UnityEngine;

namespace Unity.LEGO.Game
{
    public class InputManager : MonoBehaviour
    {
        [SerializeField] float m_VerticalLookMinSensitivity = 0.5f;
        [SerializeField, Range(0.25f, 1.0f)] float m_VerticalLookSensitivityStep = 0.25f;

        [SerializeField] float m_HorizontalLookMinimumSensitivity = 50.0f;
        [SerializeField, Range(10.0f, 100.0f)] float m_HorizontalLookSensitivityStep = 50.0f;

        CinemachineFreeLook m_FreeLookCamera;

        void Awake()
        {
            m_FreeLookCamera = FindObjectOfType<CinemachineFreeLook>();

            EventManager.AddListener<LookSensitivityUpdateEvent>(OnLookSensitivityUpdate);
        }

        void OnLookSensitivityUpdate(LookSensitivityUpdateEvent evt)
        {
            if (m_FreeLookCamera)
            {
                m_FreeLookCamera.m_XAxis.m_MaxSpeed = m_HorizontalLookMinimumSensitivity + (m_HorizontalLookSensitivityStep * evt.Value);
                m_FreeLookCamera.m_YAxis.m_MaxSpeed = m_VerticalLookMinSensitivity + (m_VerticalLookSensitivityStep * evt.Value);
            }
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<LookSensitivityUpdateEvent>(OnLookSensitivityUpdate);
        }
    }
}
