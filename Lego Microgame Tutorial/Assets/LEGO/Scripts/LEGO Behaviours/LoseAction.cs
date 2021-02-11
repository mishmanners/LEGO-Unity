using LEGOMaterials;
using Unity.LEGO.Game;
using Unity.LEGO.Behaviours.Triggers;

namespace Unity.LEGO.Behaviours.Actions
{
    public class LoseAction : ObjectiveAction
    {
        public override ObjectiveConfiguration GetDefaultObjectiveConfiguration(Trigger trigger)
        {
            ObjectiveConfiguration result = new ObjectiveConfiguration();
            
            if (trigger)
            {
                var triggerType = trigger.GetType();
                if (triggerType == typeof(PickupTrigger))
                {
                    result.Title = "Don't Pickup the Pickups!";
                    result.Description = "Don't do it!";
                    result.ProgressType = ObjectiveProgressType.Amount;
                }
                else if (triggerType == typeof(TouchTrigger))
                {
                    result.Title = "Don't Touch the Object";
                    result.Description = "You can't touch this!";
                }
                else if (triggerType == typeof(NearbyTrigger))
                {
                    result.Title = "Avoid the Object";
                    result.Description = "Avoid it!";
                }
                else if (triggerType == typeof(TimerTrigger))
                {
                    result.Title = "Finish Before the Time is Up";
                    result.Description = "Hurry up!";
                    result.ProgressType = ObjectiveProgressType.Time;
                }
                else if (triggerType == typeof(RandomTrigger))
                {
                    result.Title = "Finish Before a Random Time is Up";
                    result.Description = "Hurry up!";
                }
                else if (triggerType == typeof(InputTrigger))
                {
                    result.Title = "Don't Press the Button";
                    result.Description = "Don't push it";
                }
                else if (triggerType == typeof(CounterTrigger))
                {
                    result.Title = "Don't Complete the Objective";
                    result.Description = "Don't do it!";
                }
                else
                {
                    result.Title = "Don't Complete the Objective";
                    result.Description = "Don't do it!";
                }
            }
            else
            {
                result.Title = "Didn't Stand a Chance!";
                result.Description = "Connect a Trigger Brick to the Lose Brick to make your game easier.";
            }

            result.Lose = true;

            return result;
        }

        protected override void Reset()
        {
            base.Reset();

            m_FlashColour = MouldingColour.GetColour(MouldingColour.Id.BrightRed) * 2.0f;
            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Lose Action.png";
        }
    }
}
