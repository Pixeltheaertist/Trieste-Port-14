using Content.Server._TP.StationEvents.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._TP.StationEvents.Components;

[RegisterComponent, Access(typeof(OceanSpawnRule))]
public sealed partial class OceanSpawnRuleComponent : Component
{
    /// <summary>
    /// The entity to be spawned.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = string.Empty;

    [DataField]
    public float Amount = 5;

    [DataField]
    public float CurrentAmount;
}
