// Taken from https://github.com/emberfall-14/emberfall/pull/4/files with permission

using Content.Shared._EmberFall.Bell.Systems;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._EmberFall.Bell.Components;

/// <summary>
/// Component that marks an entity as a space elevator platform and stores its valid destinations
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedBellSystem))]
public sealed partial class BellComponent : Component
{
    /// <summary>
    /// The station this elevator platform is assigned to.
    /// </summary>
    [DataField]
    public EntityUid? Station;

    /// <summary>
    /// List of valid destinations this elevator can travel to.
    /// </summary>
    [DataField]
    public List<BellDestination> Destinations = new();

    [DataField]
    public bool CanMove = true;

    [DataField]
    public FTLState LastFTLState = FTLState.Invalid;
}

/// <summary>
/// Represents a valid destination point for a space elevator.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial record struct BellDestination
{
    /// <summary>
    /// LocId name of the destination shown in the UI.
    /// </summary>
    [DataField]
    public LocId Name;

    /// <summary>
    /// The target map ID for this destination.
    /// </summary>
    [DataField]
    public MapId Map;
}
