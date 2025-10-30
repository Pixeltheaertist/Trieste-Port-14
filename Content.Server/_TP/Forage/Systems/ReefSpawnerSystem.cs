using Content.Server._TP.Forage.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server._TP.Forage.Systems;

/// <summary>
///     The system handling the ReefSpawnerComponent.
/// </summary>
public sealed class ReefSpawnerSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReefSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    /// <summary>
    ///     Map initialization event handler. When spawned, either on round start or when placed,
    ///     the reef will spawn entities on the map.
    /// </summary>
    /// <param name="ent">ReefComponent entity></param>
    /// <param name="args">MapInitEvent Argument</param>
    private void OnMapInit(Entity<ReefSpawnerComponent> ent, ref MapInitEvent args)
    {
        // We start by setting some variables.
        // These include the entity's ReefComponent, a TransformComponent, and the coordinates.
        // Afterward we check if the xform has a grid. If not, we return.
        var reefComp = ent.Comp;
        var xform = Transform(ent);
        var origin = _transform.GetWorldPosition(xform);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var gridComp))
        {
            return;
        }

        // Now we get an enumeration of the tiles within a circle.
        // Going through the tiles afterward, we do a random probability check for each tile.
        var cir = new Circle(origin, reefComp.Radius);
        var tiles = _map.GetTilesIntersecting(xform.GridUid.Value, gridComp, cir);

        foreach (var tileRef in tiles)
        {
            if (!_random.Prob(reefComp.TileChance))
            {
                continue;
            }

            // Finally, we get the tile positions through the map system, as well as check for
            // existing entities in that position. If any exist, we return the loop.
            // If not, we run another probability and spawn an entity if it passes.
            var tilePos = _map.GridTileToLocal(xform.GridUid.Value, gridComp, tileRef.GridIndices);
            var existingEntities = _lookup.GetEntitiesIntersecting(tilePos);

            if (existingEntities.Count != 0)
                continue;

            foreach (var (proto, chance) in reefComp.Entities)
            {
                if (!_random.Prob(chance))
                    continue;

                Spawn(proto, tilePos);
                break;
            }
        }

        QueueDel(ent);
    }
}
