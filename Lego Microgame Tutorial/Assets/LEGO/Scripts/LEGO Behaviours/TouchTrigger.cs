namespace Unity.LEGO.Behaviours.Triggers
{
    public class TouchTrigger : SensoryTrigger
    {
        protected override void Reset()
        {
            base.Reset();

            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Touch Trigger.png";
        }

        protected override void Start()
        {
            base.Start();

            if (IsPlacedOnBrick())
            {
                // Add SensoryCollider to all brick colliders.
                foreach (var brick in m_ScopedBricks)
                {
                    foreach (var part in brick.parts)
                    {
                        foreach (var collider in part.colliders)
                        {
                            var sensoryCollider = LEGOBehaviourCollider.Add<SensoryCollider>(collider, m_ScopedBricks, 0.64f);
                            SetupSensoryCollider(sensoryCollider);
                        }
                    }
                }
            }
        }
    }
}
