using Content.Server._TP.StationEvents.Events;

namespace Content.Server._TP.StationEvents.Components;

[RegisterComponent, Access(typeof(OceanSpawnRule))]
public sealed partial class WhaleMigrationRuleComponent : Component;
