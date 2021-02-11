using System.Collections.Generic;
using UnityEngine;

namespace Unity.LEGO.Game
{
    public class ObjectiveManager : MonoBehaviour
    {
        List<IObjective> m_Objectives;

        bool m_Won;
        bool m_Lost;

        protected void Awake()
        {
            m_Objectives = new List<IObjective>();

            EventManager.AddListener<ObjectiveAdded>(OnObjectiveAdded);
        }

        void OnObjectiveAdded(ObjectiveAdded evt)
        {
            m_Objectives.Add(evt.Objective);
            evt.Objective.OnProgress += OnProgress;

            UpdateGameStatus();
        }

        public void OnProgress(IObjective _)
        {
            UpdateGameStatus();
        }

        void UpdateGameStatus()
        {
            m_Won = m_Objectives.Exists(objective => !objective.m_Lose);

            foreach (IObjective objective in m_Objectives)
            {
                m_Won &= (objective.IsCompleted || objective.m_Lose);
                m_Lost |= (objective.IsCompleted && objective.m_Lose);
            }
        }

        void Update()
        {
            if (m_Won || m_Lost)
            {
                Events.GameOverEvent.Win = m_Won || !m_Lost;
                EventManager.Broadcast(Events.GameOverEvent);

                Destroy(this);
            }
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<ObjectiveAdded>(OnObjectiveAdded);
        }
    }
}
