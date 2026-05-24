using Content.Server.Administration.Logs;
using Content.Server.Deathwhale;
using Content.Shared.Body.Components;
using Content.Shared.Database;
using Content.Shared.Salvage.Fulton;

namespace Content.Server.deathwhales;

public sealed class DeathWhaleSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private const float UpdateInterval = 1f;
    private float _updateTimer = 0f;

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
            // Check for prey within the DeathWhale's range
            foreach (var entity in EntityManager.EntityQuery<DeathWhaleComponent>())
            {
                var uid = entity.Owner;
                var component = EntityManager.GetComponent<DeathWhaleComponent>(uid); // Get the DeathWhaleComponent

                DeathWhaleCheck(uid, component);
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
            LogImpact.High,
            $"{uid}: {nameof(DeathWhaleComponent)} initialized.");
    }

    private void DeathWhaleCheck(EntityUid uid, DeathWhaleComponent component)
    {
        // Iterate through all entities within the DeathWhale's radius
        foreach (var prey in _lookup.GetEntitiesInRange(uid, component.Radius))
        {
            if (!EntityManager.HasComponent<BodyComponent>(prey))
            {
                continue;
            }

            if (_caughtPrey.Contains(prey))
            {
                continue;
            }

            if (EntityManager.HasComponent<DeathWhaleComponent>(prey))
            {
                continue;
            }

            // Pix pls no
            var preycaught = EnsureComp<FultonedComponent>(prey); // This will harpoon the prey and drag them up offscreen to be eaten
            preycaught.Removeable = false;
            preycaught.Beacon = uid;
            // QueueDel(prey);
        }
    }
}
