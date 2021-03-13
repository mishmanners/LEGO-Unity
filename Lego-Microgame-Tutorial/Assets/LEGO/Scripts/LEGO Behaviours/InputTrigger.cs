using System;
using Unity.LEGO.UI;
using UnityEngine;

namespace Unity.LEGO.Behaviours.Triggers
{

    public class InputTrigger : SensoryTrigger
    {
        public enum Type
        {
            Up,
            Left,
            Down,
            Right,
            Jump,
            Fire1,
            Fire2,
            Fire3,
            OtherKey
        }

        [SerializeField, Tooltip("The input to detect.")]
        Type m_Type = Type.OtherKey;

        enum Key
        {
            A = KeyCode.A,
            B = KeyCode.B,
            C = KeyCode.C,
            D = KeyCode.D,
            E = KeyCode.E,
            F = KeyCode.F,
            G = KeyCode.G,
            H = KeyCode.H,
            I = KeyCode.I,
            J = KeyCode.J,
            K = KeyCode.K,
            L = KeyCode.L,
            M = KeyCode.M,
            N = KeyCode.N,
            O = KeyCode.O,
            P = KeyCode.P,
            Q = KeyCode.Q,
            R = KeyCode.R,
            S = KeyCode.S,
            T = KeyCode.T,
            U = KeyCode.U,
            V = KeyCode.V,
            W = KeyCode.W,
            X = KeyCode.X,
            Y = KeyCode.Y,
            Z = KeyCode.Z,
        }

        [SerializeField, Tooltip("The key to detect.")]
        Key m_OtherKey = Key.E;

        public enum Enable
        {
            Always,
            WhenPlayerIsNearby,
            WhenBricksAreNearby,
            WhenTagIsNearby
        }

        [SerializeField, Tooltip("Always enable input.\nor\nEnable input when the player is nearby.\nor\nEnable input when any brick is nearby.\nor\nEnable when a tag is nearby.")]
        Enable m_Enable = Enable.WhenPlayerIsNearby;

        [SerializeField, Tooltip("The distance in LEGO modules.")]
        int m_Distance = 20;

        [SerializeField, Tooltip("Show input prompt.")]
        bool m_ShowPrompt = true;

        [SerializeField]
        GameObject m_InputPromptPrefab = default;

        InputPrompt m_InputPrompt;
        bool m_PromptActive = true;
        string m_PromptLabel;

        protected override void Reset()
        {
            base.Reset();

            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Input Trigger.png";
        }

        protected void OnValidate()
        {
            m_Distance = Mathf.Max(1, m_Distance);
        }

        protected override void Start()
        {
            base.Start();

            if (IsPlacedOnBrick())
            {
                if (m_Enable != Enable.Always)
                {
                    // Apply the correct sensing, given when to enable the input.
                    switch(m_Enable)
                    {
                        case Enable.WhenPlayerIsNearby:
                            {
                                m_Sense = Sense.Player;
                                break;
                            }
                        case Enable.WhenBricksAreNearby:
                            {
                                m_Sense = Sense.Bricks;
                                break;
                            }
                        case Enable.WhenTagIsNearby:
                            {
                                m_Sense = Sense.Tag;
                                break;
                            }
                    }

                    var colliderComponentToClone = gameObject.AddComponent<SphereCollider>();
                    colliderComponentToClone.center = m_ScopedPivotOffset;
                    colliderComponentToClone.radius = 0.0f;
                    colliderComponentToClone.enabled = false;

                    var sensoryCollider = LEGOBehaviourCollider.Add<SensoryCollider>(colliderComponentToClone, m_ScopedBricks, m_Distance * LEGOHorizontalModule);
                    SetupSensoryCollider(sensoryCollider);

                    Destroy(colliderComponentToClone);
                }
                else
                {
                    m_Distance = int.MaxValue;
                }
            }
        }

        void Update()
        {
            if (m_ShowPrompt && !m_InputPrompt)
            {
                SetupPrompt();
            }

            if (m_Repeat || !m_AlreadyTriggered)
            {
                if (m_Enable == Enable.Always || m_ActiveColliders.Count > 0)
                {
                    var visible = IsVisible();
                    UpdatePrompt(visible);

                    if (CheckInput())
                    {
                        ConditionMet();

                        if (m_ShowPrompt)
                        {
                            m_InputPrompt.Input(m_PromptLabel, m_Distance, m_Repeat, visible);
                        }
                    }
                }
                else
                {
                    UpdatePrompt(false);
                }
            }
        }

        bool CheckInput()
        {
            switch (m_Type)
            {
                case Type.Up:
                    return Input.GetAxis("Vertical") > 0.1f;
                case Type.Left:
                    return Input.GetAxis("Horizontal") < -0.1f;
                case Type.Down:
                    return Input.GetAxis("Vertical") < -0.1f;
                case Type.Right:
                    return Input.GetAxis("Horizontal") > 0.1f;
                case Type.Jump:
                    return Input.GetButtonDown("Jump");
                case Type.Fire1:
                    return Input.GetButtonDown("Fire1");
                case Type.Fire2:
                    return Input.GetButtonDown("Fire2");
                case Type.Fire3:
                    return Input.GetButtonDown("Fire3");
                case Type.OtherKey:
                    return Input.GetKeyDown((KeyCode)m_OtherKey);
                default:
                    return false;
            }
        }

        void SetupPrompt()
        {
            // Create prompt label.
            var label = m_Type <= Type.Fire3 ? Enum.GetName(typeof(Type), m_Type) : m_OtherKey.ToString();
            m_PromptLabel = m_Type >= Type.Fire1 && m_Type <= Type.Fire3 ? label.Insert(4, " ") : label;

            PromptPlacementHandler promptHandler = null;

            // Check if there is already an existing prompt in the scope.
            foreach (var brick in m_ScopedBricks)
            {
                if (brick.GetComponent<PromptPlacementHandler>())
                {
                    promptHandler = brick.GetComponent<PromptPlacementHandler>();
                }

                var inputTriggers = brick.GetComponents<InputTrigger>();

                foreach (var inputTrigger in inputTriggers)
                {
                    if (inputTrigger.m_InputPrompt)
                    {
                        m_InputPrompt = inputTrigger.m_InputPrompt;
                        break;
                    }
                }
            }

            var activeFromStart = (m_Enable == Enable.Always || m_ActiveColliders.Count > 0) && IsVisible();

            // Create a new prompt if none was found.
            if (!m_InputPrompt)
            {
                if (promptHandler == null)
                {
                    promptHandler = gameObject.AddComponent<PromptPlacementHandler>();
                }

                var go = Instantiate(m_InputPromptPrefab, promptHandler.transform);
                m_InputPrompt = go.GetComponent<InputPrompt>();

                // Get the current scoped bounds - might be different than the initial scoped bounds.
                var scopedBounds = GetScopedBounds(m_ScopedBricks, out _, out _);
                promptHandler.AddInstance(go, scopedBounds, PromptPlacementHandler.PromptType.InputPrompt, activeFromStart);
            }

            // Add this Input Trigger to the prompt.
            m_InputPrompt.AddLabel(m_PromptLabel, activeFromStart, m_Distance, promptHandler);
        }

        void UpdatePrompt(bool active)
        {
            if (m_ShowPrompt)
            {
                if (m_PromptActive != active)
                {
                    m_PromptActive = active;

                    if (active)
                    {
                        m_InputPrompt.Activate(m_PromptLabel);
                    }
                    else
                    {
                        m_InputPrompt.Deactivate(m_PromptLabel, m_Distance);
                    }
                }
            }
        }

        void OnDestroy()
        {
            if (m_InputPrompt)
            {
                UpdatePrompt(false);
            }
        }
    }
}
