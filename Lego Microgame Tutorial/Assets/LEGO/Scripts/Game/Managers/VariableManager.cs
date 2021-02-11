using System.Collections.Generic;

namespace Unity.LEGO.Game
{
    public static class VariableManager
    {
        public const string k_VariablePath = "Assets/LEGO/Scriptable Objects";

        static Dictionary<Variable, int> s_RegisteredVariables = new Dictionary<Variable, int>();

        public static void RegisterVariable(Variable variable)
        {
            if (variable && !s_RegisteredVariables.ContainsKey(variable))
            {
                s_RegisteredVariables[variable] = variable.InitialValue;

                VariableAdded evt = Events.VariableAddedEvent;
                evt.Variable = variable;
                EventManager.Broadcast(evt);
            }
        }

        public static bool IsVariableRegistered(Variable variable)
        {
            return variable && s_RegisteredVariables.ContainsKey(variable);
        }

        public static void Reset()
        {
            var variables = new List<Variable>(s_RegisteredVariables.Keys);
            foreach (var variable in variables)
            {
                s_RegisteredVariables[variable] = variable.InitialValue;

                VariableAdded evt = Events.VariableAddedEvent;
                evt.Variable = variable;
                EventManager.Broadcast(evt);
            }
        }

        public static int GetValue(Variable variable)
        {
            if (variable && s_RegisteredVariables.ContainsKey(variable))
            {
                return s_RegisteredVariables[variable];
            }

            return 0;
        }

        public static void SetValue(Variable variable, int value)
        {
            if (variable && s_RegisteredVariables.ContainsKey(variable))
            {
                s_RegisteredVariables[variable] = value;
                variable.OnUpdate?.Invoke(value);
            }
        }
    }
}
