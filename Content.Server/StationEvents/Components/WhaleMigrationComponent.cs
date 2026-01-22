using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;


namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(OceanSpawnRule))]
public sealed partial class WhaleMigrationRuleComponent : Component
{

}
