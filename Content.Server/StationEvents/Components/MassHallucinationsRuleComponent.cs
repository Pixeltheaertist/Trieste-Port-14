using Content.Server.StationEvents.Events;
using Robust.Shared.Audio;

namespace Content.Server.StationEvents.Components;

// !! TRIESTE PORT MODIFIED !! //

[RegisterComponent, Access(typeof(MassHallucinationsRule))]
public sealed partial class MassHallucinationsRuleComponent : Component
{
    /// <summary>
    /// The maximum time between incidents in seconds
    /// </summary>
    [DataField("maxTimeBetweenIncidents", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float MaxTimeBetweenIncidents;

    /// <summary>
    /// The minimum time between incidents in seconds
    /// </summary>
    [DataField("minTimeBetweenIncidents", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float MinTimeBetweenIncidents;

    [DataField("maxSoundDistance", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float MaxSoundDistance;

    // TRIESTE SPECIFIC
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool SweetwaterOnly;

    [DataField("sounds", required: true)]
    public SoundSpecifier Sounds = default!;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> AffectedEntities = new();
}
