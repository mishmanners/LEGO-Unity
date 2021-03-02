using Unity.LEGO.Game;
using UnityEngine;

namespace Unity.LEGO.Behaviours.Actions
{
    public class CounterAction : RepeatableAction
    {
        public enum Operator
        {
            Add,
            Subtract,
            Multiply,
            Set
        }

        [SerializeField, Tooltip("The variable to modify.")]
        Variable m_Variable = null;

        [SerializeField, Tooltip("The operator to apply between value and variable.")]
        Operator m_Operator = Operator.Add;

        [SerializeField, Tooltip("The value to use with the operator to modify variable.")]
        int m_Value = 1;

        float m_Time;
        bool m_HasUpdatedVariable;

        protected override void Reset()
        {
            base.Reset();

            m_Repeat = false;
            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Counter Action.png";
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            m_Pause = Mathf.Max(m_Pause, 0.0f);
        }

        protected override void Start()
        {
            base.Start();

            VariableManager.RegisterVariable(m_Variable);
        }

        protected void LateUpdate()
        {
            if (m_Active && m_Variable != null)
            {
                m_Time += Time.deltaTime;

                if (!m_HasUpdatedVariable)
                {
                    UpdateVariable();
                    m_HasUpdatedVariable = true;
                }

                if (m_Time >= m_Pause)
                {
                    m_Time -= m_Pause;
                    m_HasUpdatedVariable = false;
                    m_Active = m_Repeat;
                }
            }
        }

        void UpdateVariable()
        {
            switch (m_Operator)
            {
                case Operator.Add:
                    {
                        VariableManager.SetValue(m_Variable, VariableManager.GetValue(m_Variable) + m_Value);
                        break;
                    }
                case Operator.Subtract:
                    {
                        VariableManager.SetValue(m_Variable, VariableManager.GetValue(m_Variable) - m_Value);
                        break;
                    }
                case Operator.Multiply:
                    {
                        VariableManager.SetValue(m_Variable, VariableManager.GetValue(m_Variable) * m_Value);
                        break;
                    }
                case Operator.Set:
                    {
                        VariableManager.SetValue(m_Variable, m_Value);
                        break;
                    }
            }
        }
    }
}
