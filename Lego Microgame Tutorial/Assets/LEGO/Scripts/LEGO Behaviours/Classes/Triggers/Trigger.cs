using LEGOMaterials;
using System.Collections.Generic;
using Unity.LEGO.Behaviours.Actions;
using Unity.LEGO.Game;
using UnityEngine;

namespace Unity.LEGO.Behaviours.Triggers
{
    public abstract class Trigger : LEGOBehaviour
    {
        public System.Action OnProgress;
        public System.Action OnActivate;

        public int Progress;
        public int Goal;

        public enum Target
        {
            ConnectedActions,
            SpecificActions
        }

        [SerializeField]
        protected List<Condition> m_Conditions = new List<Condition>();

        [SerializeField, Tooltip("Trigger actions on connected bricks.\nor\nTrigger a list of specific actions.")]
        protected Target m_Target = Target.ConnectedActions;

        [SerializeField, Tooltip("The list of actions to trigger.")]
        protected List<Action> m_SpecificTargetActions = new List<Action>();

        [SerializeField, Tooltip("Trigger continuously.")]
        protected bool m_Repeat = true;

        protected HashSet<Action> m_TargetedActions = new HashSet<Action>();
        protected bool m_AlreadyTriggered;

        public HashSet<Action> GetTargetedActions()
        {
            var result = new HashSet<Action>();

            switch (m_Target)
            {
                case Target.ConnectedActions:
                {
                    if (IsPlacedOnBrick())
                    {
                        var connectedBricks = m_Brick.GetConnectedBricks();
                        foreach (var connectedBrick in connectedBricks)
                        {
                            result.UnionWith(connectedBrick.GetComponents<Action>());
                        }
                    }
                    result.UnionWith(GetComponents<Action>());
                    break;
                }
                case Target.SpecificActions:
                {
                    result.UnionWith(m_SpecificTargetActions);
                    break;
                }
            }

            return result;
        }

        public (Target, int) GetTargetAndActionsCount() { return (m_Target, m_SpecificTargetActions.Count); }

        protected override void Reset()
        {
            base.Reset();

            m_FlashColour = MouldingColour.GetColour(MouldingColour.Id.BrickYellow) * 2.0f;
        }

        protected override void Awake()
        {
            base.Awake();

            if (IsPlacedOnBrick())
            {
                m_TargetedActions = GetTargetedActions();
            }
        }

        protected virtual void Start()
        {
            foreach (var condition in m_Conditions)
            {
                VariableManager.RegisterVariable(condition.Variable);
            }
        }

        protected void ConditionMet()
        {
            if (m_Repeat || !m_AlreadyTriggered)
            {
                if (AdditionalConditionsMet())
                {
                    Flash();

                    List<Action> winAndLoseActions = new List<Action>();

                    foreach (var action in m_TargetedActions)
                    {
                        if (action)
                        {
                            action.Activate();

                            // Keep track of Win and Lose Actions. We only want to trigger them once, so we have to remove them.
                            if (action is ObjectiveAction)
                            {
                                winAndLoseActions.Add(action);
                            }
                        }
                    }

                    // Remove Win and Lose Actions.
                    foreach (var action in winAndLoseActions)
                    {
                        m_TargetedActions.Remove(action);
                    }

                    OnActivate?.Invoke();

                    m_AlreadyTriggered = true;
                }
            }
        }

        bool AdditionalConditionsMet()
        {
            foreach (var condition in m_Conditions)
            {
                if (VariableManager.IsVariableRegistered(condition.Variable))
                {
                    switch (condition.Type)
                    {
                        case Condition.ComparisonType.GreaterThan:
                            if (VariableManager.GetValue(condition.Variable) <= condition.Value)
                            {
                                return false;
                            }
                            break;
                        case Condition.ComparisonType.LessThan:
                            if (VariableManager.GetValue(condition.Variable) >= condition.Value)
                            {
                                return false;
                            }
                            break;
                        case Condition.ComparisonType.EqualTo:
                            if (VariableManager.GetValue(condition.Variable) != condition.Value)
                            {
                                return false;
                            }
                            break;
                        case Condition.ComparisonType.NotEqualTo:
                            if (VariableManager.GetValue(condition.Variable) == condition.Value)
                            {
                                return false;
                            }
                            break;
                    }
                }
            }

            return true;
        }
    }
}
