using Content.Server._TP.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Gravity;
using Content.Shared.Parallax;

namespace Content.Server._TP.StationEvents.Events;

/// <summary>
///     Handles the whale migration event, including changing the parallax background.
/// </summary>
public sealed partial class WhaleMigrationRule : StationEventSystem<WhaleMigrationRuleComponent>
{
    protected override void Started(EntityUid uid,
        WhaleMigrationRuleComponent comp,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        var query = EntityQueryEnumerator<TriesteAirspaceComponent>();
        while (query.MoveNext(out var triesteUid, out _))
        {
            var parallax = EnsureComp<ParallaxComponent>(triesteUid);
            Log.Info("Sky Whale Event - Start");
            parallax.Parallax = "SkyWhales";
            Dirty(triesteUid, parallax);
        }
    }

    protected override void Ended(EntityUid uid,
        WhaleMigrationRuleComponent comp,
        GameRuleComponent gameRule,
        GameRuleEndedEvent args)
    {
        base.Ended(uid, comp, gameRule, args);

        var query = EntityQueryEnumerator<TriesteAirspaceComponent>();
        while (query.MoveNext(out var triesteUid, out _))
        {
            var parallax = EnsureComp<ParallaxComponent>(triesteUid);
            Log.Info("Sky Whale Event - End");
            parallax.Parallax = "Sky";
            Dirty(triesteUid, parallax);
        }
    }
}
