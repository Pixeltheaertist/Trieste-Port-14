// Taken from https://github.com/emberfall-14/emberfall/pull/4/files with permission

using System.Linq;
using Content.Server.Shuttles.Events;
using Content.Server.Station.Systems;
using Content.Shared.Shuttles.Components;
using Content.Shared.Station.Components;
using Content.Shared.TP14.Bell.Components;
using Content.Shared.TP14.Bell.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Map.Components;

namespace Content.Server._TP.Bell;

public sealed class BellSystem : SharedBellSystem
{
    [Dependency] private readonly BellConsoleSystem _console = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BellComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BellComponent, FTLStartedEvent>(OnFTLStarted);
        SubscribeLocalEvent<BellComponent, FTLCompletedEvent>(OnFTLCompleted);
        SubscribeLocalEvent<StationGridAddedEvent>(OnStationGridAdded);
    }

    private void OnMapInit(Entity<BellComponent> ent, ref MapInitEvent args)
    {
        // Find all valid FTL destinations this elevator can reach
        var query = EntityQueryEnumerator<FTLDestinationComponent, MapComponent>();
        while (query.MoveNext(out var mapUid, out var dest, out var map))
        {
            Log.Info($"Adding map: {mapUid} to destinations");
            if (!dest.Enabled)
                continue;

            ent.Comp.Destinations.Add(new BellDestination
            {
                Name = Name(mapUid),
                Map = map.MapId,
            });
            Log.Info($"Added map: {mapUid} to destinations");
        }
    }

    private void OnFTLStarted(Entity<BellComponent> ent, ref FTLStartedEvent args)
    {
        _console.UpdateConsolesUsing(ent);
    }

    private void OnFTLCompleted(Entity<BellComponent> ent, ref FTLCompletedEvent args)
    {
        _console.UpdateConsolesUsing(ent);
    }

    private void OnStationGridAdded(StationGridAddedEvent args)
    {
        var uid = args.GridId;
        if (!TryComp<BellComponent>(uid, out var comp))
            return;
        Log.Info("Adding station to list");

        // only add the destination once
        if (comp.Station != null)
            return;

        if (_station.GetOwningStation(uid) is not { } station || !TryComp<StationDataComponent>(station, out var data))
            return;
        Log.Info("Still adding station to list");

        // add the source station as a destination
        comp.Station = station;
        comp.Destinations.Add(new BellDestination
        {
            Name = Name(station),
            Map = Transform(data.Grids.First()).MapID,
        });
    }
}
