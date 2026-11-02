using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Shared._TP.Maps.Components;

[RegisterComponent]
public sealed partial class MultiMapManagerComponent : Component
{
    /// <summary>
    ///     A dictionary of the selected maps to load.
    ///     <para>string: What to name the station. Used by the PDA and other menus.</para>
    ///     <para>ProtoId: The "GameMap" prototype to load. These are NOT the maps files.</para>
    /// </summary>
    [DataField]
    public Dictionary<string, ProtoId<GameMapPrototype>> Maps { get; set; } = new();
}
