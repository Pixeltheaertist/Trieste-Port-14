using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._TP.Weather;

[RegisterComponent]
public sealed partial class LightningMarkerComponent : Component
{
    // The lightning prototype this marker should spawn
    [DataField]
    public EntProtoId LightningPrototype = "AdminInstantEffectThunder";

    // The sound object prototype this marker should spawn
    [DataField]
    public EntProtoId LightningSoundPrototype = "AdminInstantEffectThunderSound";

    // The lightning prototype this marker should spawn during flash storms
    [DataField]
    public EntProtoId StormLightningPrototype = "Eldritch";

    // The lightning prototype this marker should spawn if the lightning 'strikes' during a flash storm.
    [DataField]
    public EntProtoId StormStrikePrototype = "Storm";

    // The range in which lightning will occur
    [DataField]
    public float ThunderRange = 70f;

    // Used by weather systems. Dictates whether this marker will create lightning or not. False = yes.
    [DataField]
    public bool Cleared = false;

    // How often (in seconds) the lightning will occur
    [DataField]
    public float ThunderFrequency = 8f;

    // Decides whether this marker will be using the normal LightningPrototype or the StormLightningPrototype
    [DataField]
    public bool StormMode;

    [DataField]
    public float NextStrike = 8f;
}
