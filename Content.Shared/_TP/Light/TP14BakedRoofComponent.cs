using Robust.Shared.GameStates;

namespace Content.Shared._TP.Light;

/// <summary>
/// Put on a grid alongside <see cref="Content.Shared.Light.Components.RoofComponent"/>.
/// Means every <see cref="Content.Shared.Light.Components.IsRoofComponent"/> on the grid has been written into the
/// roof bitmask, so <see cref="Content.Shared.Light.EntitySystems.SharedRoofSystem"/> can skip its per-tile entity
/// lookup. The bitmask is kept up to date by TP14RoofCacheSystem on the server.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TP14BakedRoofComponent : Component;
