using System.Collections.Generic;
using Unity.LEGO.Game;
using UnityEngine;

namespace Unity.LEGO.Behaviours.Triggers
{
    public class CounterTrigger : Trigger
    {
        [SerializeField]
        Variable m_DefaultVariable = null;

        protected override void Reset()
        {
            base.Reset();

            m_Conditions = new List<Condition> { new Condition() { Variable = m_DefaultVariable } };
            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Counter Trigger.png";
        }

        void Update()
        {
            ConditionMet();
        }
    }
}
