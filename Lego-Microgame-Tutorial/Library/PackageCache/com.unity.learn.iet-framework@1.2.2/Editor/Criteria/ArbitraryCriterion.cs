using System;
using UnityEditor;
using UnityEngine;
using SerializableCallback;

namespace Unity.InteractiveTutorials
{
    /// <summary>
    /// Allows tutorial author to specify arbitrary completion criterion.
    /// Create a new ScriptableObject for your criterion and provide e.g. "bool IsMyCriterionSatisfied()"
    /// function as Callback to evalute the criterion. Provide a function that completes your criterion as
    /// AutoCompleteCallback if you wish to be able to auto-complete the page.
    /// </summary>
    public class ArbitraryCriterion : Criterion
    {
        [Serializable]
        public class BoolCallback : SerializableCallback<bool>
        {
        }

        public BoolCallback Callback { get => m_Callback; set => m_Callback = value; }
        [SerializeField]
        BoolCallback m_Callback = default;

        public BoolCallback AutoCompleteCallback { get => m_AutoCompleteCallback; set => m_AutoCompleteCallback = value; }
        [SerializeField]
        BoolCallback m_AutoCompleteCallback = default;

        protected override bool EvaluateCompletion()
        {
            // TODO revisit the logic here -- should AutoCompleteCallback take a precedence over Callback?
            // Or set some internal state to completed state?
            if (m_Callback != null)
                return m_Callback.Invoke();
            else
                return false;
        }

        public override void StartTesting()
        {
            UpdateCompletion();

            EditorApplication.update += UpdateCompletion;
        }

        public override void StopTesting()
        {
            EditorApplication.update -= UpdateCompletion;
        }

        public override bool AutoComplete()
        {
            if (m_AutoCompleteCallback != null)
                return m_AutoCompleteCallback.Invoke();
            else
                return false;
        }
    }
}
