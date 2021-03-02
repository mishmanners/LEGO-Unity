using System.Collections.Generic;
using UnityEngine;
using Unity.LEGO.Behaviours.Triggers;
using Unity.LEGO.Game;

namespace Unity.LEGO.Behaviours.Actions
{
    public abstract class ObjectiveAction : Action
    {
        public abstract ObjectiveConfiguration GetDefaultObjectiveConfiguration(Trigger trigger);

        [SerializeField]
        List<ObjectiveConfiguration> m_ObjectiveConfigurations = new List<ObjectiveConfiguration>();
        [SerializeField]
        List<Trigger> m_Triggers = new List<Trigger>();

        public override void Activate()
        {
            PlayAudio(spatial: false, destroyWithAction: false);

            base.Activate();

            m_Active = false;
        }

        protected override void Start()
        {
            base.Start();

            if (IsPlacedOnBrick())
            {
                ObjectiveConfiguration objectiveConfiguration;

                var targetingTriggers = GetTargetingTriggers();

                if (targetingTriggers.Count == 0)
                {
                    objectiveConfiguration = GetDefaultObjectiveConfiguration(null);
                    AddObjective(null, objectiveConfiguration.Title, objectiveConfiguration.Description, objectiveConfiguration.ProgressType, objectiveConfiguration.Lose, objectiveConfiguration.Hidden);
                }

                // Find all Triggers and create UI for them.
                foreach (var trigger in targetingTriggers)
                {
                    var triggerIndex = m_Triggers.IndexOf(trigger);
                    if (triggerIndex >= 0)
                    {
                        objectiveConfiguration = m_ObjectiveConfigurations[triggerIndex];
                    }
                    else
                    {
                        objectiveConfiguration = GetDefaultObjectiveConfiguration(trigger);
                    }

                    // Add Objective to this game object.
                    AddObjective(trigger, objectiveConfiguration.Title, objectiveConfiguration.Description, objectiveConfiguration.ProgressType, objectiveConfiguration.Lose, objectiveConfiguration.Hidden);
                }
            }
        }

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            var gizmoBounds = GetGizmoBounds();

            if (GetTargetingTriggers().Count == 0)
            {
                Gizmos.DrawIcon(gizmoBounds.center + Vector3.up * 2, "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Warning.png");
            }
        }

        void AddObjective(Trigger trigger, string title, string description, ObjectiveProgressType progressType, bool lose, bool hidden)
        {
            var objective = gameObject.AddComponent<Objective>();
            objective.m_Trigger = trigger;
            objective.m_Title = title;
            objective.m_Description = description;
            objective.m_ProgressType = progressType;
            objective.m_Lose = lose;
            objective.m_Hidden = hidden;
        }
    }
}
