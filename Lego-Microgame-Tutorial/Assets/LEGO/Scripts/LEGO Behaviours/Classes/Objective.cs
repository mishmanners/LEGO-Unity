using System;
using UnityEngine;
using Unity.LEGO.Behaviours.Triggers;
using Unity.LEGO.Game;

namespace Unity.LEGO.Behaviours
{
    public class Objective : MonoBehaviour, IObjective
    {
        public Trigger m_Trigger;
        public string m_Title { get; set; }
        public string m_Description { get; set; }
        public ObjectiveProgressType m_ProgressType { get; set; }
        public bool m_Lose { get; set; }
        public bool m_Hidden { get; set; }
        public bool IsCompleted { get; private set; }

        public Action<IObjective> OnProgress { get; set; }

        public string GetProgress() {
            switch(m_ProgressType)
            {
                case ObjectiveProgressType.None:
                    {
                        return string.Empty;
                    }
                case ObjectiveProgressType.Amount:
                    {
                        return m_Trigger.Progress + "/" + m_Trigger.Goal;
                    }
                case ObjectiveProgressType.Time:
                    {
                        var seconds = m_Trigger.Goal - m_Trigger.Progress;
                        var minutes = seconds / 60;
                        seconds -= 60 * minutes;
                        if (minutes > 0)
                        {
                            return minutes.ToString() + ":" + seconds.ToString("D2");
                        }
                        return seconds.ToString();
                    }
            }

            return string.Empty;
        }

        void Start()
        {
            ObjectiveAdded evt = Events.ObjectiveAddedEvent;
            evt.Objective = this;
            EventManager.Broadcast(evt);

            if (m_Trigger)
            {
                m_Trigger.OnProgress += Progress;
                m_Trigger.OnActivate += Activate;
            }
            else
            {
                Activate();
            }
        }

        void Progress()
        {
            OnProgress?.Invoke(this);
        }

        void Activate()
        {
            if (IsCompleted)
            {
                return;
            }

            IsCompleted = true;
            OnProgress?.Invoke(this);
        }
    }
}
