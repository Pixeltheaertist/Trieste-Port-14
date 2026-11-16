using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._TP.NanoTraitor;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared._TP.NanoTraitor;
using Content.Shared.Antag;
using Content.Shared.Codewords;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Rules.Components;
using Content.Shared.Mind;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Components;
using Content.Shared.Store.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._TP;

[TestFixture]
public sealed class NanoTraitorRuleTest : GameTest
{
    private const string NanoTraitorRule = "TP14TraitorRuleNanoTraitor";
    private const string NanoTraitorSpec = "TP14AntagSpecNanoTraitor";
    private const string NanoTraitorIssuer = "TP14ObjIssuerNanoTrasen";
    private const string BluespaceCrystal = "TP14CurrencyBluespaceCrystal";
    private const string Telecrystal = "Telecrystal";
    private static readonly ProtoId<NpcFactionPrototype> NanoTrasenTraitorFaction = "NanoTrasenTraitor";
    private static readonly ProtoId<NpcFactionPrototype> NanoTrasenFaction = "NanoTrasen";
    private static readonly ProtoId<NpcFactionPrototype> SyndicateFaction = "Syndicate";

    public override PoolSettings PoolSettings => new()
    {
        Dirty = true,
        DummyTicker = false,
        Connected = true,
        InLobby = true,
    };

    [Test]
    public async Task TestNanoTraitor()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var ticker = server.System<ServerGameTicker>();
        var mindSys = server.System<MindSystem>();
        var roleSys = server.System<RoleSystem>();
        var factionSys = server.System<NpcFactionSystem>();
        var antagSys = server.System<AntagSelectionSystem>();
        var codewordSys = server.System<CodewordSystem>();

        await server.WaitPost(() =>
        {
            ticker.ToggleReadyAll(true);
            ticker.StartRound();
        });
        await pair.RunTicksSync(10);
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));

        // Same path as the admin verb.
        await server.WaitPost(() =>
        {
            antagSys.ForceMakeAntag<NanoTraitorRuleComponent>(server.PlayerMan.Sessions.First(),
                NanoTraitorRule,
                NanoTraitorSpec);
        });
        await pair.RunTicksSync(10);

        await server.WaitAssertion(() =>
        {
            var player = pair.Player!.AttachedEntity!.Value;
            var mind = mindSys.GetMind(player)!.Value;
            var mindComp = entMan.GetComponent<MindComponent>(mind);

            Assert.That(roleSys.MindIsAntagonist(mind));
            Assert.That(roleSys.MindHasRole<NanoTraitorRoleComponent>(mind), Is.True);
            Assert.That(roleSys.MindHasRole<TraitorRoleComponent>((mind, mindComp), out var traitorRole), Is.True);

            // Faction swap is done by the upstream traitor rule, configured from YAML.
            Assert.That(factionSys.IsMember(player, NanoTrasenTraitorFaction), Is.True);
            Assert.That(factionSys.IsMember(player, NanoTrasenFaction), Is.False);
            Assert.That(factionSys.IsMember(player, SyndicateFaction), Is.False);

            // Exactly one uplink store, owned by this mind and paid in BC.
            var stores = new List<StoreComponent>();
            var query = entMan.EntityQueryEnumerator<StoreComponent>();
            while (query.MoveNext(out _, out var store))
            {
                if (store.AccountOwner == mind)
                    stores.Add(store);
            }
            Assert.That(stores, Has.Count.EqualTo(1), "NanoTraitor should own exactly one uplink store.");
            Assert.That(stores[0].Balance.ContainsKey(BluespaceCrystal), Is.True);
            Assert.That(stores[0].Balance.ContainsKey(Telecrystal), Is.False);

            // Codewords come from the NT faction.
            var codewords = entMan.GetComponent<RoleCodewordComponent>(traitorRole!.Value);
            var ntCodewords = codewordSys.GetCodewords("NanoTrasenTraitor");
            Assert.That(codewords.RoleCodewords.Codewords, Is.EquivalentTo(ntCodewords));
            var ruleQuery = entMan.EntityQueryEnumerator<NanoTraitorRuleComponent>();
            Assert.That(ruleQuery.MoveNext(out var ruleComp), "NanoTraitor rule should exist.");
            Assert.That(codewords.RoleCodewords.Color, Is.EqualTo(ruleComp.CodewordColor),
                "Codeword color should be overwritten after the upstream traitor rule runs.");

            // Briefing was replaced with the NT version.
            var briefing = entMan.GetComponent<RoleBriefingComponent>(traitorRole.Value.Owner);
            Assert.That(briefing.Briefing.Id, Is.Not.Empty);

            // Objectives are issued by NanoTrasen.
            Assert.That(mindComp.Objectives, Is.Not.Empty);
            foreach (var objective in mindComp.Objectives)
            {
                Assert.That(entMan.GetComponent<ObjectiveComponent>(objective).Issuer.Id, Is.EqualTo(NanoTraitorIssuer),
                    $"Objective {entMan.ToPrettyString(objective)} is not issued by NanoTrasen.");
            }

            // The Syndicate traitor rule shouldn't have been touched.
            var syndicateRules = entMan.EntityQueryEnumerator<TraitorRuleComponent>();
            while (syndicateRules.MoveNext(out var uid, out var rule))
            {
                if (entMan.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID == NanoTraitorRule)
                    Assert.That(rule.TraitorMinds, Does.Contain(mind));
                else
                    Assert.That(rule.TraitorMinds, Does.Not.Contain(mind));
            }
        });
    }
}
