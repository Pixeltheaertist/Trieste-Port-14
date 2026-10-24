using Content.Server._TP.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared._TP.DeathWhales;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Map;

namespace Content.Server._TP.StationEvents.Events;

public sealed partial class OceanSpawnRule : StationEventSystem<OceanSpawnRuleComponent>
{
    protected override void Added(Entity<OceanSpawnRuleComponent, GameRuleComponent> ent, ref GameRuleAddedEvent args)
    {
        base.Added(ent, ref args);
        if (!Station.TryGetRandomStation<StationEventEligibleComponent>(out _))
            return;

        var locations = EntityQueryEnumerator<DeathWhaleSpawnLocationComponent, TransformComponent>(); // TODO: Make choosing the location possible.
        var validLocations = new List<EntityCoordinates>();

        while (locations.MoveNext(out var _, out _, out var transform))
        {
            validLocations.Add(transform.Coordinates);

            if (ent.Comp1.CurrentAmount >= ent.Comp1.Amount) break;
            SpawnAtPosition(ent.Comp1.Prototype, transform.Coordinates);
            ent.Comp1.CurrentAmount += 1;
        }

        if (validLocations.Count == 0)
            return;

        foreach (var location in validLocations)
        {
            if (ent.Comp1.CurrentAmount >= ent.Comp1.Amount) break;

            SpawnAtPosition(ent.Comp1.Prototype, location);
            ent.Comp1.CurrentAmount += 1;
        }
    }

    protected override void Ended(Entity<OceanSpawnRuleComponent> rule, ref GameRuleEndedEvent args)
    {
        base.Ended(rule, ref args);
        rule.Comp.CurrentAmount = 0f;

        foreach (var whales in EntityQuery<DeathWhaleComponent>())
        {
            var whaleUid = whales.Owner;
            QueueDel(whaleUid);  // Deleting the whale entity, they've left!!
        }
    }
}
