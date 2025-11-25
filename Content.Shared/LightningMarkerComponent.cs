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

        // The sound object prototype this marker should spawn
        [DataField("lightningSoundPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string LightningSoundPrototype = "AdminInstantEffectThunderSound";

        // The lightning prototype this marker should spawn during flash storms
        [DataField("stormLightningPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string StormLightningPrototype = "Eldritch";

        // The lightning prototype this marker should spawn if the lightning 'strikes' during a flash storm.
        [DataField("stormStrikePrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string StormStrikePrototype = "Storm";

        // The range in which lightning will occur
        [DataField("thunderRange")]
        public float ThunderRange = 70f;

        [DataField("cleared")]
        public bool Cleared = false; // Used by weather systems. Dictates whether this marker will create lightning or not. False = yes.

        // How often (in seconds) the lightning will occur
        [DataField("thunderFrequency")]
        public float ThunderFrequency = 8f;

        // Decides whether this marker will be using the normal LightningPrototype or the StormLightningPrototype
        [DataField("stormMode")]
        public bool StormMode = false;
    }
}
