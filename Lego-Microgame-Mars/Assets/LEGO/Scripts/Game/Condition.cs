using System;
using UnityEngine;

namespace Unity.LEGO.Game
{
    [Serializable]
    public class Condition
    {
        public enum ComparisonType
        {
            GreaterThan,
            LessThan,
            EqualTo,
            NotEqualTo
        }

        [Tooltip("The variable to check.")]
        public Variable Variable = null;

        [Tooltip("The comparison to apply to variable and value.")]
        public ComparisonType Type = ComparisonType.GreaterThan;

        [Tooltip("The value to compare with the variable.")]
        public int Value = 1;
    }
}