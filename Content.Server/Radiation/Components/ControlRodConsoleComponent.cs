namespace Content.Server.Radiation.Components
{
    [RegisterComponent]
    public sealed partial class ControlRodConsoleComponent : Component
    {
        public const string RodPort = "ControlRodSender";
        [ViewVariables]
        public List<EntityUid> ControlRods = new List<EntityUid>();
        /// Maximum distance between console and one if its machines
        [DataField("maxDistance")]
        public float MaxDistance = 8f;
        public Dictionary<int,bool> RodsInRange = new Dictionary<int, bool>();
    }
}
