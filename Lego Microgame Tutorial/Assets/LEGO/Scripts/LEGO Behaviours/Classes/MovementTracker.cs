using LEGOModelImporter;
using UnityEngine;

namespace Unity.LEGO.Behaviours
{
    public class MovementTracker : MonoBehaviour
    {
        Vector3 m_PreviousAverageGroupPosition = Vector3.zero;

        ModelGroup[] m_Groups;

        void Awake()
        {
            m_Groups = GetComponentsInChildren<ModelGroup>();

            // Find the average group position.
            if (m_Groups.Length > 0)
            {
                foreach (var group in m_Groups)
                {
                    m_PreviousAverageGroupPosition += group.transform.position;
                }
                m_PreviousAverageGroupPosition /= m_Groups.Length;
            }
        }

        public void UpdateModelPosition()
        {
            if (m_Groups.Length > 0)
            {
                // Find the current average group position.
                var currentAverageGroupPosition = Vector3.zero;
                foreach (var group in m_Groups)
                {
                    currentAverageGroupPosition += group.transform.position;
                }
                currentAverageGroupPosition /= m_Groups.Length;

                // Move the model and model groups according to the change in average.
                var diff = currentAverageGroupPosition - m_PreviousAverageGroupPosition;

                transform.position += diff;

                foreach (var group in m_Groups)
                {
                    group.transform.position -= diff;
                }

                m_PreviousAverageGroupPosition = currentAverageGroupPosition;
            }
        }
    }
}