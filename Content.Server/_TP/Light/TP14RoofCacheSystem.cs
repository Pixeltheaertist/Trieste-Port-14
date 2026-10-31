using Content.Server.Light.EntitySystems;
using Content.Shared._TP.Light;
using Content.Shared.Gravity;
using Content.Shared.Light.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._TP.Light;

/// <summary>
/// Gives the Trieste grid a per-tile roof and keeps its roof bitmask in sync with the
/// <see cref="IsRoofComponent"/> entities on it (TP14MarkerInside), so roofed tiles block map light without the client
/// looking up entities on every tile, every frame.
/// </summary>
/// <remarks>
/// The bitmask is updated whenever a roof entity starts up or shuts down. If you toggle a roof at runtime
/// (e.g. a roof collapsing), use <see cref="SetRoofEnabled"/> so the cache follows.
/// </remarks>
public sealed partial class TP14RoofCacheSystem : EntitySystem
{
    [Dependency] private RoofSystem _roof = default!;
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<TP14BakedRoofComponent> _bakedQuery = default!;

    private readonly HashSet<Entity<IsRoofComponent>> _roofSet = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
        SubscribeLocalEvent<IsRoofComponent, ComponentStartup>(OnRoofStartup);
        SubscribeLocalEvent<IsRoofComponent, ComponentShutdown>(OnRoofShutdown);
        SubscribeLocalEvent<IsRoofComponent, EntityTerminatingEvent>(OnRoofTerminating);
    }

    private void OnGridInit(GridInitializeEvent ev)
    {
        TryBakeGrid(ev.EntityUid);
    }

    private void OnRoofStartup(Entity<IsRoofComponent> ent, ref ComponentStartup args)
    {
        if (!ent.Comp.Enabled)
            return;

        if (TryGetTile(ent.Owner, out var grid, out var index))
            _roof.SetRoof((grid.Owner, grid.Comp, null), index, true);
    }

    // Deleting an entity detaches it from its grid before its components shut down, so by ComponentShutdown we no
    // longer know which tile it was on. Handle deletion here instead, while it's still attached.
    private void OnRoofTerminating(Entity<IsRoofComponent> ent, ref EntityTerminatingEvent args)
    {
        ClearRoof(ent.Owner);
    }

    // Only covers the IsRoof component being removed from an entity that's otherwise sticking around.
    private void OnRoofShutdown(Entity<IsRoofComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent.Owner))
            return;

        ClearRoof(ent.Owner);
    }

    private void ClearRoof(EntityUid uid)
    {
        if (TryGetTile(uid, out var grid, out var index) && !TerminatingOrDeleted(grid.Owner))
            RefreshTile(grid, index, ignore: uid);
    }

    /// <summary>
    /// Turns a roof entity on or off and updates the roof cache for its tile.
    /// </summary>
    public void SetRoofEnabled(Entity<IsRoofComponent?> ent, bool enabled)
    {
        if (!Resolve(ent, ref ent.Comp, false) || ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent.Owner, ent.Comp);

        if (TryGetTile(ent.Owner, out var grid, out var index))
            RefreshTile(grid, index);
    }

    /// <summary>
    /// Re-checks a tile for enabled roof entities and writes the result into the grid's roof bitmask.
    /// </summary>
    public void RefreshTile(Entity<MapGridComponent> grid, Vector2i index, EntityUid? ignore = null)
    {
        _roofSet.Clear();
        _lookup.GetLocalEntitiesIntersecting(grid.Owner, index, _roofSet);

        var rooved = false;
        foreach (var roof in _roofSet)
        {
            if (roof.Owner == ignore || !roof.Comp.Enabled || TerminatingOrDeleted(roof.Owner))
                continue;

            rooved = true;
            break;
        }

        _roof.SetRoof((grid.Owner, grid.Comp, null), index, rooved);
    }

    /// <summary>
    /// Gets the tile a roof entity sits on, if it's on a grid whose roof we cache.
    /// </summary>
    private bool TryGetTile(EntityUid uid, out Entity<MapGridComponent> grid, out Vector2i index)
    {
        grid = default;
        index = default;

        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid || !_gridQuery.TryComp(gridUid, out var gridComp))
            return false;

        // Roof entities can start up before their grid's GridInitializeEvent handler has run, so bake here too.
        if (!_bakedQuery.HasComp(gridUid) && !TryBakeGrid(gridUid))
            return false;

        grid = (gridUid, gridComp);
        index = _maps.LocalToTile(gridUid, gridComp, xform.Coordinates);
        return true;
    }

    /// <summary>
    /// Trieste grids get a per-tile roof instead of an implicit one (see ShuttleSystem), with the bitmask cached.
    /// </summary>
    private bool TryBakeGrid(EntityUid gridUid)
    {
        if (!HasComp<TriesteComponent>(gridUid))
            return false;

        EnsureComp<RoofComponent>(gridUid);
        EnsureComp<TP14BakedRoofComponent>(gridUid);
        return true;
    }
}
