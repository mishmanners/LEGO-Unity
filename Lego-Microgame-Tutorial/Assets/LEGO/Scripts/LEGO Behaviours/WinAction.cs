using Unity.LEGO.Game;
using Unity.LEGO.Behaviours.Triggers;

namespace Unity.LEGO.Behaviours.Actions
{
    public class WinAction : ObjectiveAction
    {
        public override ObjectiveConfiguration GetDefaultObjectiveConfiguration(Trigger trigger)
        {
            ObjectiveConfiguration result = new ObjectiveConfiguration();

            if (trigger)
            {
                var triggerType = trigger.GetType();
                if (triggerType == typeof(PickupTrigger))
                {
                    result.Title = "Collect all the Pickups";
                    result.Description = "Go get 'em!";
                    result.ProgressType = ObjectiveProgressType.Amount;
                }
                else if (triggerType == typeof(TouchTrigger))
                {
                    result.Title = "Touch the Object";
                    result.Description = "Touch it!";
                }
                else if (triggerType == typeof(NearbyTrigger))
                {
                    result.Title = "Get to the Object";
                    result.Description = "Get there!";
                }
                else if (triggerType == typeof(TimerTrigger))
                {
                    result.Title = "Survive";
                    result.Description = "Hang in there!";
                    result.ProgressType = ObjectiveProgressType.Time;
                }
                else if (triggerType == typeof(RandomTrigger))
                {
                    result.Title = "Survive for a Random Time";
                    result.Description = "Hang in there!";
                }
                else if (triggerType == typeof(InputTrigger))
                {
                    result.Title = "Press the Button";
                    result.Description = "Push it!";
                }
                else if (triggerType == typeof(CounterTrigger))
                {
                    result.Title = "Complete the Objective";
                    result.Description = "Just do it!";
                }
                else
                {
                    result.Title = "Complete the Objective";
                    result.Description = "Just do it!";
                }
            }
            else
            {
                result.Title = "Easy as Pie!";
                result.Description = "Connect a Trigger Brick to the Win Brick to make this objective more challenging.";
            }

            return result;
        }

        protected override void Reset()
        {
            base.Reset();

            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Win Action.png";
        }
    }
}
