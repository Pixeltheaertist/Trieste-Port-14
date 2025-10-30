using Robust.Shared.Prototypes;

namespace Content.Server._TP.Forage.Components;

/// <summary>
///     This component spawns an array of entities in a radius, with a supplied chance.
/// </summary>
[RegisterComponent, EntityCategory("Spawner")]
public sealed partial class ReefSpawnerComponent : Component
{
    /// <summary>
    ///     The list of entities to spawn, and their chance TO spawn.
    ///     This is separate from the total spawn chance.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, float> Entities = new();

    /// <summary>
    ///     The total over-all spawn chance per tile.
    /// </summary>
    [DataField]
    public float TileChance = 0.33f;

    /// <summary>
    ///     Radius (in tiles) to spawn entities on. 0 will target only the tile the entity is on.
    /// </summary>
    [DataField]
    public float Radius = 3f;

}
