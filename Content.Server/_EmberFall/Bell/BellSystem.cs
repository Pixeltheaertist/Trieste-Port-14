using System.Linq;
using Content.Server.Shuttles.Events;
using Content.Server.Station.Systems;
using Content.Shared._EmberFall.Bell.Components;
using Content.Shared._EmberFall.Bell.Systems;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Station.Components;
using Robust.Shared.Map.Components;

namespace Content.Server._EmberFall.Bell;

/// <summary>
///     The systems handling the Bell.
///     Taken from https://github.com/emberfall-14/emberfall/pull/4/files with permission
/// </summary>
public sealed partial class BellSystem : SharedBellSystem
{
    [Dependency] private readonly BellConsoleSystem _console = default!;
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

    /// <summary>
    ///     Update method for the Bell System.
    ///     This is messy, but it has forced my hand without making new events.
    /// </summary>
    /// <param name="frameTime"></param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Check for FTL state changes and update consoles
        // We also update the bell's current state so that it can be displayed on the UI
        var query = EntityQueryEnumerator<BellComponent>();
        while (query.MoveNext(out var uid, out var bell))
        {
            var currentState = TryComp<FTLComponent>(uid, out var ftl)
                ? ftl.State
                : FTLState.Available;

            if (bell.LastFTLState != currentState)
            {
                bell.LastFTLState = currentState;
                _console.UpdateConsolesUsing(uid);
            }
        }
    }
}
