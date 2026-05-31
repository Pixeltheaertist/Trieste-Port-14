using Content.Server.Administration.Logs;
using Content.Shared._TP.DeathWhales;
using Content.Shared.Body.Components;
using Content.Shared.Database;
using Content.Shared.Salvage.Fulton;
using Robust.Shared.Timing;

namespace Content.Server._TP.DeathWhales;

public sealed partial class DeathWhaleSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    private const float UpdateInterval = 1f;
    private float _updateTimer;

    // Store the caught prey UIDs
    private readonly HashSet<EntityUid> _caughtPrey = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeathWhaleComponent, ComponentInit>(OnCompInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _updateTimer += frameTime;

        if (_updateTimer >= UpdateInterval)
        {
            var query = EntityQueryEnumerator<DeathWhaleComponent>();
            while (query.MoveNext(out var uid, out var component))
            {
                // Temporarily commented out for a FULL rework, eventually. Does work though!
                // DeathWhaleCheck(uid, component);
            }

            // Reset the update timer
            _updateTimer = 0f;
        }
    }

    // Log message when the component is initialized
    private void OnCompInit(EntityUid uid, DeathWhaleComponent component, ComponentInit args)
    {
        _adminLogger.Add(
            LogType.EntitySpawn,
            LogImpact.Extreme,
            $"{uid}: {nameof(DeathWhaleComponent)} initialized.");
    }

    private void DeathWhaleCheck(EntityUid uid, DeathWhaleComponent component)
    {
        // Iterate through all entities within the DeathWhale's radius
        foreach (var prey in _lookup.GetEntitiesInRange(uid, component.Radius))
        {
            if (!EntityManager.HasComponent<BodyComponent>(prey))
                continue;

            if (_caughtPrey.Contains(prey))
                continue;

            if (EntityManager.HasComponent<DeathWhaleComponent>(prey))
                continue;

            _caughtPrey.Add(prey);
        }

        var caught = _caughtPrey;
        foreach (var prey in caught)
        {
            var fulton = EnsureComp<FultonedComponent>(prey);
            fulton.Beacon = uid;
            fulton.FultonDuration = TimeSpan.FromSeconds(1);
            fulton.NextFulton = _timing.CurTime;
            fulton.Removeable = false;

            Timer.Spawn(TimeSpan.FromSeconds(0.25),
                () =>
                {
                    if (EntityManager.EntityExists(prey))
                    {
                        QueueDel(prey);
                    }
                });

            _caughtPrey.Remove(prey);
        }
    }
}
