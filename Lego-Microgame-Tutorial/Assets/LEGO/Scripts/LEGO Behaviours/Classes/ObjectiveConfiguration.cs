using System;
using Unity.LEGO.Game;

namespace Unity.LEGO.Behaviours
{
    [Serializable]
    public class ObjectiveConfiguration
    {
        public string Title = "Title";
        public string Description = "Description";
        public ObjectiveProgressType ProgressType;
        public bool Lose;
        public bool Hidden;
    }
}
