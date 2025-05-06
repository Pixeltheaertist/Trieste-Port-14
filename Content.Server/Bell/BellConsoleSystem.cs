// Taken from https://github.com/emberfall-14/emberfall/pull/4/files with permission
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.TP14.Bell;
using Content.Shared.TP14.Bell.Components;
using Content.Shared.TP14.Bell.Systems;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Timing;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.TP14.Bell.Systems;

public sealed class BellConsoleSystem : SharedBellConsoleSystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DockEvent>(OnDock);
        SubscribeLocalEvent<UndockEvent>(OnUndock);

        // UI subscriptions
        Subs.BuiEvents<BellConsoleComponent>(BellUiKey.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnOpened);
                subs.Event<DockingConsoleFTLMessage>(OnFTL);
            });
    }

    private void OnDock(DockEvent args)
    {
        UpdateConsoles(args.GridAUid, args.GridBUid);
    }

    private void OnUndock(UndockEvent args)
    {
        UpdateConsoles(args.GridAUid, args.GridBUid);
    }

    private void OnOpened(Entity<BellConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        // Need to verify elevator platform still exists
        if (TerminatingOrDeleted(ent.Comp.Platform))
            UpdatePlatform(ent);

        UpdateUI(ent);
    }

    /// <summary>
    /// Updates consoles connected to platforms on either grid.
    /// </summary>
    private void UpdateConsoles(EntityUid gridA, EntityUid gridB)
    {
        UpdateConsolesUsing(gridA);
        UpdateConsolesUsing(gridB);
    }

    /// <summary>
    /// Updates all console UIs that control a specific elevator platform.
    /// </summary>
    public void UpdateConsolesUsing(EntityUid platform)
    {
        if (!HasComp<BellComponent>(platform))
            return;

        var query = EntityQueryEnumerator<BellConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Platform == platform)
                UpdateUI((uid, comp));
        }
    }

    private void UpdateUI(Entity<BellConsoleComponent> ent)
    {
        if (ent.Comp.Platform is not { } platform)
            return;

        var ftlState = FTLState.Available;
        StartEndTime ftlTime = default;
        var destinations = new List<BellDestination>();

        // Get available destinations
        if (TryComp<BellComponent>(platform, out var elevator))
            destinations = elevator.Destinations;

        // Get current FTL state if applicable
        if (TryComp<FTLComponent>(platform, out var ftl))
        {
            ftlState = ftl.State;
            ftlTime = _shuttle.GetStateTime(ftl);
        }

        var state = new BellConsoleState(ftlState, ftlTime, destinations);
        _ui.SetUiState(ent.Owner, BellUiKey.Key, state);
    }

    private void OnFTL(Entity<BellConsoleComponent> ent, ref DockingConsoleFTLMessage args)
    {
        if (ent.Comp.Platform is not { } platform ||
            !TryComp<BellComponent>(platform, out var elevator))
            return;
            
        if (!elevator.CanMove)
        {
            return;
        }

        if (args.Index < 0 || args.Index > elevator.Destinations.Count)
            return;

        var dest = elevator.Destinations[args.Index];
        var map = dest.Map;

        // Validate transfer is possible
        if (map == Transform(platform).MapID || !_shuttle.CanFTLTo(platform, map, ent))
            return;

        if (FindLargestGrid(map) is not { } grid)
            return;

        _shuttle.FTLToDock(platform, Comp<ShuttleComponent>(platform), grid, priorityTag: ent.Comp.DockTag);
    }

    /// <summary>
    /// Finds the largest grid in a map, prioritizing station grids.
    /// </summary>
    private EntityUid? FindLargestGrid(MapId map)
    {
        if (_station.GetStationInMap(map) is { } station)
            return _station.GetLargestGrid(Comp<StationDataComponent>(station));

        EntityUid? largestGrid = null;
        var largestSize = 0f;

        var query = EntityQueryEnumerator<MapGridComponent, TransformComponent>();
        while (query.MoveNext(out var gridUid, out var grid, out var xform))
        {
            if (xform.MapID != map)
                continue;

            var size = grid.LocalAABB.Size.LengthSquared();
            if (size < largestSize)
                continue;

            largestSize = size;
            largestGrid = gridUid;
        }

        return largestGrid;
    }

    private void UpdatePlatform(Entity<BellConsoleComponent> ent)
    {
        var hadPlatform = ent.Comp.HasPlatform;
        ent.Comp.Platform = FindPlatform(ent.Comp.PlatformWhitelist);
        ent.Comp.HasPlatform = ent.Comp.Platform != null;

        if (ent.Comp.HasPlatform != hadPlatform)
            Dirty(ent);
    }

    private EntityUid? FindPlatform(EntityWhitelist whitelist)
    {
        var query = EntityQueryEnumerator<BellComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (_whitelist.IsValid(whitelist, uid))
                return uid;
        }

        return null;
    }
}
