using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Server.Deathwhale;
using Content.Shared.Gravity;
using Content.Shared.Parallax;

namespace Content.Server.StationEvents.Events
{
    public sealed class WhaleMigrationRule : StationEventSystem<WhaleMigrationRuleComponent>
    {
        protected override void Started(EntityUid uid, WhaleMigrationRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
        {
            base.Started(uid, comp, gameRule, args);

            foreach (var triestes in EntityManager.EntityQuery<TriesteAirspaceComponent>())
            {
                var target = triestes.Owner;

                if (target != null)
                {
                    Log.Error("Trieste found");
                    if (TryComp<ParallaxComponent>(target, out var parallax))
                    {
                        Log.Error("Parallax found");
                        parallax.Parallax = "SkyWhales";
                    }
                }
            }

        }

        protected override void Ended(EntityUid uid, WhaleMigrationRuleComponent comp, GameRuleComponent gameRule, GameRuleEndedEvent args)
        {

            foreach (var triestes in EntityManager.EntityQuery<TriesteAirspaceComponent>())
            {
                var target = triestes.Owner;

                if (target != null)
                {
                    if (TryComp<ParallaxComponent>(target, out var parallax))
                    {
                        parallax.Parallax = "Sky";
                        return;
                    }
                }
            }

        }
    }
}
