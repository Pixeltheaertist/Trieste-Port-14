using Robust.Shared.GameStates;

namespace Content.Shared._TP.Mechs.Components;

/// <summary>
///     Marks an entity as an Expeditionary Pilot.
///     Networked so the client can evaluate the Rotund's pilot whitelist.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ExpedPilotComponent : Component
{
}
