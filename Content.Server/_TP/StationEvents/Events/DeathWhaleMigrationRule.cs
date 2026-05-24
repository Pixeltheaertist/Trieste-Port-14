using Content.Server.Deathwhale;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;

namespace Content.Server._TP.StationEvents.Events
{
    public sealed class OceanSpawnRule : StationEventSystem<Components.OceanSpawnRuleComponent>
    {
        protected override void Started(EntityUid uid, Components.OceanSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
        {
            base.Started(uid, comp, gameRule, args);

            var amount = comp.Amount;

            if (!TryGetRandomStation(out _))
                return;

            var locations = EntityQueryEnumerator<Components.DeathWhaleSpawnLocationComponent, TransformComponent>(); // TODO: Make choosing the location possible.
            var validLocations = new List<EntityCoordinates>();

            while (locations.MoveNext(out var _, out _, out var transform))
            {
                validLocations.Add(transform.Coordinates);

                if (comp.CurrentAmount >= amount) break;
                Spawn(comp.Prototype, transform.Coordinates);
                comp.CurrentAmount += 1;
            }

            if (validLocations.Count == 0)
                return;

            foreach (var location in validLocations)
            {
                if (comp.CurrentAmount >= amount) break;

                Spawn(comp.Prototype, location);
                comp.CurrentAmount += 1;
            }
        }

        protected override void Ended(EntityUid uid, Components.OceanSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleEndedEvent args)
        {
            base.Ended(uid, comp, gameRule, args);
            comp.CurrentAmount = 0f;

            foreach (var whales in EntityManager.EntityQuery<DeathWhaleComponent>())
            {
                var whaleUid = whales.Owner;
                QueueDel(whaleUid);  // Deleting the whale entity, they've left!!
            }
        }
    }
}
