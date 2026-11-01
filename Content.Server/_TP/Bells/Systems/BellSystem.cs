using System.Linq;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Shared._TP.Bells.Components;
using Content.Shared._TP.Bells.Prototypes;
using Content.Shared.GameTicking.Events;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._TP.Bells.Systems;

/// <summary>
///     Server-side entity system for the Bell component.
/// </summary>
public sealed partial class BellSystem : EntitySystem
{
    [Dependency] private BellConsoleSystem _bellConsole = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private MapLoaderSystem _mapLoader = default!;
    [Dependency] private ShuttleSystem _shuttle = default!;

    private readonly Dictionary<ProtoId<BellTypePrototype>, List<BellPortEntry>> _ports = new();
    private readonly Dictionary<ProtoId<BellTypePrototype>, List<EntityUid>> _bells = new();

    private void SpawnBell(ProtoId<BellTypePrototype> bellType)
    {
        if (!ProtoMan.TryIndex(bellType, out var bellProto))
            return;

        if (_bells.TryGetValue(bellType, out var existing) && existing.Count > 0)
            return;

        _map.CreateMap(out var mapId);

        if (!_mapLoader.TryLoadGrid(mapId, bellProto.ShuttlePath, out var grid))
        {
            Log.Error($"Failed to load bell shuttle for {bellType} from {bellProto.ShuttlePath}");
            return;
        }

        var comp = new BellComponent { BellType = bellType };
        AddComp(grid.Value, comp);
    }

    [SubscribeLocalEvent]
    private void OnShuttleTag(EntityUid uid, BellPortComponent component, ref FTLTagEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        args.Tag = ProtoMan.Index(component.BellType).PriorityTag;
    }

    [SubscribeLocalEvent]
    private void OnPortStartup(EntityUid uid, BellPortComponent comp, ComponentStartup args)
    {
        if (Transform(uid).GridUid is not { } grid)
            return;

        _ports.GetOrNew(comp.BellType)
            .Add(new BellPortEntry(uid, grid, Loc.GetString(comp.PortName)));
    }

    [SubscribeLocalEvent]
    private void OnRoundStarting(RoundStartingEvent ev)
    {
        foreach (var bellProto in ProtoMan.EnumeratePrototypes<BellTypePrototype>())
        {
            SpawnBell(bellProto.ID);
        }
    }

    [SubscribeLocalEvent]
    private void OnPortShutdown(EntityUid uid, BellPortComponent comp, ComponentShutdown args)
    {
        if (_ports.TryGetValue(comp.BellType, out var list))
            list.RemoveAll(e => e.Door == uid);
    }

    [SubscribeLocalEvent]
    private void OnStartup(EntityUid uid, BellComponent comp, ComponentStartup args)
    {
        comp.Unsummoned = comp.CurrentPort == null;
        comp.CurrentPort = null;
        comp.TransitStartTime = null;
        comp.TransitEndTime = null;

        _bells.GetOrNew(comp.BellType).Add(uid);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(EntityUid uid, BellComponent comp, ComponentShutdown args)
    {
        if (_bells.TryGetValue(comp.BellType, out var list))
            list.Remove(uid);
    }

    [SubscribeLocalEvent]
    private void OnBellFtlStarted(EntityUid uid, BellComponent comp, ref FTLStartedEvent args)
    {
        RefreshConsolesFor(comp.BellType);
    }

    [SubscribeLocalEvent]
    private void OnBellFtlCompleted(EntityUid uid, BellComponent comp, ref FTLCompletedEvent args)
    {
        RefreshConsolesFor(comp.BellType);
    }

    public void RefreshConsolesFor(ProtoId<BellTypePrototype> bellType)
    {
        var query = EntityQueryEnumerator<BellConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.BellType == bellType)
                _bellConsole.UpdateUi(uid, comp);
        }
    }

    public BellStatus GetBellStatus(ProtoId<BellTypePrototype> bellType)
    {
        if (!_bells.TryGetValue(bellType, out var list) || list.Count == 0)
            return new BellStatus(null, false, null, null, null, null);

        var bellUid = list[0];
        var comp = Comp<BellComponent>(bellUid);

        FTLState? ftlState = null;
        TimeSpan? start = null;
        TimeSpan? end = null;

        if (TryComp<FTLComponent>(bellUid, out var ftl))
        {
            ftlState = ftl.State;
            start = ftl.StateTime.Start;
            end = ftl.StateTime.End;
        }

        return new BellStatus(bellUid, comp.Locked, comp.CurrentPort, ftlState, start, end);
    }

    public bool TravelTo(ProtoId<BellTypePrototype> bellType, EntityUid destinationDoor)
    {
        var status = GetBellStatus(bellType);
        if (status.Bell is not { } bellUid)
            return false;

        if (status.Locked)
            return false;

        return StartTravel(bellUid, Comp<BellComponent>(bellUid), destinationDoor);
    }

    /// <summary>
    ///     Sends the bell to a port, ignoring the lock. Fails if the bell is already in FTL (or cooling down).
    /// </summary>
    private bool StartTravel(EntityUid bellUid, BellComponent comp, EntityUid destinationDoor)
    {
        var bellType = comp.BellType;

        // FTLToDock silently does nothing while the bell still has an FTLComponent, so don't pretend it worked.
        if (HasComp<FTLComponent>(bellUid))
            return false;

        if (!TryComp<BellPortComponent>(destinationDoor, out var destPort) || destPort.BellType != bellType)
            return false;

        if (!TryComp<DockingComponent>(destinationDoor, out _))
            return false;

        if (Transform(destinationDoor).GridUid is not { } destinationGrid)
            return false;

        if (!TryComp<ShuttleComponent>(bellUid, out var shuttleComp))
            return false;

        if (!ProtoMan.TryIndex(bellType, out var bellProto))
            return false;

        comp.CurrentPort = destinationDoor;

        _shuttle.FTLToDock(
            bellUid,
            shuttleComp,
            destinationGrid,
            bellProto.StartupTime,
            bellProto.HyperspaceTime,
            bellProto.PriorityTag);

        return true;
    }

    public void SetLockedState(ProtoId<BellTypePrototype> bellType, bool locked)
    {
        if (!_bells.TryGetValue(bellType, out var list))
            return;

        foreach (var bellUid in list)
        {
            if (TryComp<BellComponent>(bellUid, out var comp))
                comp.Locked = locked;
        }
    }

    #region Flash Storm

    /// <summary>
    ///     Locks every bell and sends it to its storm shelter port (see <see cref="BellPortComponent.StormShelter"/>).
    /// </summary>
    public void StartStormLockdown()
    {
        var query = EntityQueryEnumerator<BellComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Locked = true;
            comp.StormRecallPending = !TryStormRecall(uid, comp);
            RefreshConsolesFor(comp.BellType);
        }
    }

    /// <summary>
    ///     Unlocks every bell. They stay wherever they ended up.
    /// </summary>
    public void EndStormLockdown()
    {
        var query = EntityQueryEnumerator<BellComponent>();
        while (query.MoveNext(out _, out var comp))
        {
            comp.Locked = false;
            comp.StormRecallPending = false;
            RefreshConsolesFor(comp.BellType);
        }
    }

    /// <summary>
    ///     Returns true once the bell is at (or heading to) its shelter, or if it has no shelter to go to.
    /// </summary>
    private bool TryStormRecall(EntityUid uid, BellComponent comp)
    {
        if (FindStormShelter(comp.BellType) is not { } shelter)
        {
            Log.Warning($"No storm shelter port for bell type {comp.BellType}; it will only be locked.");
            return true;
        }

        if (comp.CurrentPort == shelter && !HasComp<FTLComponent>(uid))
            return true;

        return StartTravel(uid, comp, shelter);
    }

    private EntityUid? FindStormShelter(ProtoId<BellTypePrototype> bellType)
    {
        if (!_ports.TryGetValue(bellType, out var ports))
            return null;

        foreach (var port in ports)
        {
            if (TryComp<BellPortComponent>(port.Door, out var portComp) && portComp.StormShelter)
                return port.Door;
        }

        return null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Retry bells that were mid-FTL or cooling down when the storm hit.
        var query = EntityQueryEnumerator<BellComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.StormRecallPending || HasComp<FTLComponent>(uid))
                continue;

            if (!TryStormRecall(uid, comp))
                continue;

            comp.StormRecallPending = false;
            RefreshConsolesFor(comp.BellType);
        }
    }

    #endregion

    public IReadOnlyList<BellPortEntry> GetDestinations(ProtoId<BellTypePrototype> bellType)
    {
        if (!_ports.TryGetValue(bellType, out var list))
            return Array.Empty<BellPortEntry>();

        return list
            .GroupBy(e => e.Grid)
            .Select(g => g.First())
            .ToList();
    }
}

public readonly record struct BellPortEntry(EntityUid Door, EntityUid Grid, string Name);

public readonly record struct BellStatus(
    EntityUid? Bell,
    bool Locked,
    EntityUid? CurrentPort,
    FTLState? FtlState,
    TimeSpan? PhaseStart,
    TimeSpan? PhaseEnd);