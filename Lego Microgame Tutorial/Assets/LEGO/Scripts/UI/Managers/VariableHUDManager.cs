using System.Collections.Generic;
using Unity.LEGO.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.LEGO.UI
{
    public class VariableHUDManager : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Tooltip("The UI panel containing the layoutGroup for displaying variables.")]
        RectTransform m_VariablePanel = default;

        HashSet<Game.Variable> m_ShownVariables = new HashSet<Game.Variable>();

        const int s_TopMargin = 10;
        const int s_Spacing = 10;
        float m_NextY;

        protected void Awake()
        {
            EventManager.AddListener<VariableAdded>(OnVariableAdded);
        }

        void OnVariableAdded(VariableAdded evt)
        {
            if (evt.Variable.UseUI && evt.Variable.UIPrefab)
            {
                // Instantiate the UI element for the new variable if not already shown.
                if (!m_ShownVariables.Contains(evt.Variable))
                {
                    m_ShownVariables.Add(evt.Variable);

                    GameObject go = Instantiate(evt.Variable.UIPrefab, m_VariablePanel);

                    // Initialise the variable element.
                    Variable variable = go.GetComponent<Variable>();
                    variable.Initialize(evt.Variable.Name, evt.Variable.InitialValue.ToString());

                    // Force layout rebuild to get height of variable UI.
                    LayoutRebuilder.ForceRebuildLayoutImmediate(go.GetComponent<RectTransform>());

                    // Position the variable.
                    var rectTransform = go.GetComponent<RectTransform>();
                    rectTransform.anchoredPosition = new Vector2(rectTransform.sizeDelta.x, m_NextY - s_TopMargin);
                    m_NextY -= rectTransform.sizeDelta.y + s_Spacing;

                    evt.Variable.OnUpdate += variable.OnUpdate;
                }
            }
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<VariableAdded>(OnVariableAdded);
        }
    }
}
