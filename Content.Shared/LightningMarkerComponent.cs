using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;

namespace Content.Shared.Gravity
{
    [RegisterComponent]
    public sealed partial class LightningMarkerComponent : Component
    {
        // The lightning prototype this marker should spawn
        [DataField("lightningPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string LightningPrototype = "AdminInstantEffectThunder";

        [DataField("stormLightningPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string StormLightningPrototype = "Eldritch";

        [DataField("stormStrikePrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string StormStrikePrototype = "Storm";

        // The range in which lightning will occur
        [DataField("thunderRange")]
        public float ThunderRange = 70f;

        [DataField("cleared")]
        public bool Cleared = false;

        // How often (in seconds) the lightning will occur
        [DataField("thunderFrequency")]
        public float ThunderFrequency = 8f;

        [DataField("stormMode")]
        public bool StormMode = false;
    }
}
