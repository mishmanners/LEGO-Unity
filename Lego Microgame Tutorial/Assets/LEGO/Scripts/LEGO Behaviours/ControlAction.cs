using Unity.LEGO.Behaviours.Actions;
using Unity.LEGO.Behaviours.Controls;
using Unity.LEGO.Game;
using UnityEngine;

namespace Unity.LEGO.Behaviours {
    public class ControlAction : MovementAction
    {
        enum ControlType
        {
            Hovercraft,
            Aircraft
        }

        [SerializeField, Tooltip("Control like a hovercraft.\nor\nControl like an aircraft.")]
        ControlType m_ControlType = ControlType.Hovercraft;

        public enum InputType
        {
            Tank,
            Direct,
            Strafe
        }

        [SerializeField, Tooltip("Turn relative to self.\nor\nTurn relative to direction.\nor\nTurn relative to camera.")]
        InputType m_InputType = InputType.Direct;

        [SerializeField]
        int m_MinSpeed = -20;
        [SerializeField]
        int m_MaxSpeed = 20;
        [SerializeField, Tooltip("The idle speed in LEGO modules per second.")]
        int m_IdleSpeed = 0;
        [SerializeField, Range(0, 720), Tooltip("The rotation speed in degrees per second.")]
        int m_RotationSpeed = 360;

        [SerializeField, Tooltip("Make other bricks behave as if this is the player.")]
        bool m_IsPlayer = true;

        bool m_CanMoveOnY;
        bool m_IgnoreYAxis;
        bool m_CameraRelativeMovement;
        bool m_CameraAlignedRotation;

        Camera m_MainCamera;

        ControlMovement m_ControlMovement;

        Vector3 m_TargetDirection;

        protected override void Reset()
        {
            base.Reset();

            m_Repeat = false;
            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Control Action.png";
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            var minSpeed = m_MinSpeed;
            if (m_InputType == InputType.Direct)
            {
                minSpeed = 0;
                m_MaxSpeed = Mathf.Max(0, m_MaxSpeed);
            }

            m_IdleSpeed = Mathf.Clamp(m_IdleSpeed, minSpeed, m_MaxSpeed);
        }

        protected override void Start()
        {
            base.Start();

            m_MainCamera = Camera.main;

            if (IsPlacedOnBrick())
            {
                SetupInputType();

                AddControlMovement();

                if (m_IsPlayer)
                {
                    // Tag all the part colliders to make other LEGO Behaviours act as if this is the player.
                    foreach (var brick in m_ScopedBricks)
                    {
                        foreach (var part in brick.parts)
                        {
                            foreach (var collider in part.colliders)
                            {
                                collider.gameObject.tag = "Player";
                                collider.gameObject.layer = LayerMask.NameToLayer("Player");
                            }
                        }
                    }
                }
            }
        }

        protected void Update()
        {
            if (m_Active)
            {
                HandleInput();

                Collision();

                // Move and rotate control movement.
                m_ControlMovement.Movement(m_TargetDirection, m_MinSpeed, m_MaxSpeed, m_IdleSpeed);
                m_ControlMovement.Rotation(m_TargetDirection, m_RotationSpeed);

                // Update model position.
                m_MovementTracker.UpdateModelPosition();
            }
        }

        void Collision()
        {
            if (base.IsColliding())
            {
                // Find the penetration direction most opposite of the current velocity.
                var velocity = m_ControlMovement.GetVelocity();
                var mostOppositeDirection = Vector3.zero;
                var mostNegativeDotProduct = float.MaxValue;
                foreach (var activeColliderPair in m_ActiveColliderPairs)
                {
                    if (Physics.ComputePenetration(activeColliderPair.Item1, activeColliderPair.Item1.transform.position, activeColliderPair.Item1.transform.rotation, activeColliderPair.Item2, activeColliderPair.Item2.transform.position, activeColliderPair.Item2.transform.rotation, out Vector3 direction, out _))
                    {
                        var dotProduct = Vector3.Dot(velocity, direction);
                        if (dotProduct < mostNegativeDotProduct)
                        {
                            mostOppositeDirection = direction;
                            mostNegativeDotProduct = dotProduct;
                        }
                    }
                }

                if (mostOppositeDirection.sqrMagnitude > 0.0f)
                {
                    m_ControlMovement.Collision(mostOppositeDirection);
                }
                else
                {
                    if (velocity.sqrMagnitude > 0.0f)
                    {
                        // We have a collision but no penetration, so just use the normalized velocity as collision direction.
                        m_ControlMovement.Collision(velocity.normalized);
                    }
                    else
                    {
                        // We have a collision but no penetration and no velocity, so just use the target direction as collision direction.
                        m_ControlMovement.Collision(m_TargetDirection);
                    }
                }
            }
        }

        void SetupInputType()
        {
            switch (m_InputType)
            {
                case InputType.Tank:
                    m_CameraRelativeMovement = false;
                    m_CameraAlignedRotation = false;
                    break;
                case InputType.Direct:
                    m_CameraRelativeMovement = true;
                    m_CameraAlignedRotation = false;
                    break;
                case InputType.Strafe:
                    m_CameraRelativeMovement = true;
                    m_CameraAlignedRotation = true;
                    break;
            }
        }

        void AddControlMovement()
        {
            switch (m_ControlType)
            {
                case ControlType.Hovercraft:
                    m_ControlMovement = gameObject.AddComponent<Hovercraft>();
                    m_CanMoveOnY = true;
                    m_IgnoreYAxis = true;
                    break;
                case ControlType.Aircraft:
                    m_ControlMovement = gameObject.AddComponent<Aircraft>();
                    m_CanMoveOnY = true;
                    m_IgnoreYAxis = false;
                    break;
            }

            m_ControlMovement.Setup(m_Group, m_ScopedBricks, m_scopedPartRenderers, m_BrickPivotOffset, m_ScopedBounds, m_CameraAlignedRotation, m_CameraRelativeMovement);
        }

        void HandleInput()
        {
            Vector3 right;
            Vector3 forward;

            if (m_CameraRelativeMovement)
            {
                right = m_MainCamera.transform.right;
                forward = m_MainCamera.transform.forward;
            }
            else
            {
                right = transform.right;
                forward = transform.forward;
            }

            if (m_IgnoreYAxis)
            {
                right.y = 0.0f;
                forward.y = 0.0f;
            }

            m_TargetDirection = m_InputType == InputType.Tank ? Vector3.zero : right * Input.GetAxisRaw("Horizontal");
            m_TargetDirection += forward * Input.GetAxisRaw("Vertical");

            // Move up or down.
            if (m_CanMoveOnY)
            {
                if (Input.GetButton("Fire1"))
                {
                    m_TargetDirection += Vector3.up;
                }
                if (Input.GetButton("Fire2"))
                {
                    m_TargetDirection += Vector3.down;
                }
            }

            if (m_TargetDirection.sqrMagnitude > 0.0f)
            {
                m_TargetDirection.Normalize();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (m_IsPlayer)
            {
                GameOverEvent evt = Events.GameOverEvent;
                evt.Win = false;
                EventManager.Broadcast(evt);
            }
        }
    }
}
