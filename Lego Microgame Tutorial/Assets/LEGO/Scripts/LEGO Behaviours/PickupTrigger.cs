using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.Behaviours.Triggers
{
    public class PickupTrigger : Trigger
    {
        public enum Mode
        {
            AllPickups,
            AmountOfPickups,
            SpecificPickups
        }

        [SerializeField, Tooltip("Trigger when all pickups are collected.\nor\nTrigger when an amount of pickups are collected.\nor\nTrigger when specific pickups are collected.")]
        Mode m_Mode = Mode.AllPickups;

        [SerializeField, Tooltip("The amount of pickups to collect.")]
        int m_AmountModeCount = 1;

        [SerializeField, Tooltip("The list of pickups to collect.")]
        List<PickupAction> m_SpecificModePickupActions = new List<PickupAction>();

        List<PickupAction> m_PickupActions = new List<PickupAction>();
        int m_PreviousProgress;

        public Mode GetMode()
        {
            return m_Mode;
        }

        public ReadOnlyCollection<PickupAction> GetSpecificPickupActions()
        {
            return m_SpecificModePickupActions.AsReadOnly();
        }

        protected override void Reset()
        {
            base.Reset();

            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Pickup Trigger.png";
        }

        protected void OnValidate()
        {
            m_AmountModeCount = Mathf.Max(1, m_AmountModeCount);
        }

        protected override void Start()
        {
            base.Start();

            if (IsPlacedOnBrick())
            {
                // Find relevant Pickup Actions.
                if (m_Mode == Mode.SpecificPickups)
                {
                    m_PickupActions.AddRange(m_SpecificModePickupActions);
                }
                else
                {
                    m_PickupActions.AddRange(FindObjectsOfType<PickupAction>());
                }

                // Set up listener and count number of valid Pickup Actions.
                var validPickupActions = 0;
                foreach (var pickupAction in m_PickupActions)
                {
                    if (pickupAction)
                    {
                        pickupAction.OnCollected += PickupCollected;
                        validPickupActions++;
                    }
                }

                // Register amount of pickups left to collect.
                if (m_Mode == Mode.AmountOfPickups)
                {
                     Goal = m_AmountModeCount;
                }
                else
                {
                     Goal = validPickupActions;
                }
            }
        }

        void Update()
        {
            if (m_PreviousProgress != Progress)
            {
                if (Progress <  Goal)
                {
                    OnProgress?.Invoke();
                }

                m_PreviousProgress = Progress;
            }

            if (Progress >=  Goal)
            {
                ConditionMet();
            }
        }

        void PickupCollected(PickupAction pickup)
        {
            Progress++;
            pickup.OnCollected -= PickupCollected;
        }

        void OnDestroy()
        {
            foreach (var pickup in m_PickupActions)
            {
                if (pickup)
                {
                    pickup.OnCollected -= PickupCollected;
                }
            }
        }
    }
}
