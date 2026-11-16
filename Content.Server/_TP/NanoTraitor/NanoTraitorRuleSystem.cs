using System.Linq;
using System.Text;
using Content.Server.GameTicking.Rules;
using Content.Server.PDA.Ringer;
using Content.Server.Store.Systems;
using Content.Server.StoreDiscount.Systems;
using Content.Server.Traitor.Uplink;
using Content.Shared.Antag;
using Content.Shared.Codewords;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking.Rules.Components;
using Content.Shared.Implants;
using Content.Shared.Mind;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.Store.Components;
using Robust.Shared.Map;

namespace Content.Server._TP.NanoTraitor;

/// <summary>
///     Runs after the upstream traitor rule on rules that have <see cref="NanoTraitorRuleComponent"/>.
///     Upstream already handles the mind role, codewords, faction swap and objectives.
///     This system adds the NanoTrasen uplink and briefing, and recolors the codewords.
/// </summary>
public sealed partial class NanoTraitorRuleSystem : EntitySystem
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private CodewordSystem _codeword = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private SharedSubdermalImplantSystem _implant = default!;
    [Dependency] private StoreSystem _store = default!;
    [Dependency] private UplinkSystem _uplink = default!;

    [SubscribeLocalEvent(after: [typeof(ServerTraitorRuleSystem)])]
    private void OnAntagSelected(Entity<NanoTraitorRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        var traitor = args.EntityUid;

        if (!TryComp<TraitorRuleComponent>(ent, out var traitorRule))
        {
            Log.Error($"{ToPrettyString(ent)} has {nameof(NanoTraitorRuleComponent)} but no {nameof(TraitorRuleComponent)}.");
            return;
        }

        if (!_mind.TryGetMind(traitor, out var mindId, out var mind))
            return;

        var codewords = traitorRule.GiveCodewords
            ? _codeword.GetCodewords(traitorRule.CodewordFactionPrototypeId)
            : null;

        Note[]? code = null;
        var hasUplink = false;
        if (ent.Comp.GiveUplink)
            hasUplink = TryGiveUplink(ent.Comp, traitor, mindId, out code);

        if (ent.Comp.GiveBriefing)
        {
            var briefing = BuildBriefing(ent.Comp, codewords, hasUplink, code, shortVersion: false);
            _antag.SendBriefing(traitor, briefing, null, ent.Comp.GreetSound);
        }

        if (!_role.MindHasRole<TraitorRoleComponent>((mindId, mind), out var traitorRole))
            return;

        // The character menu briefing, replacing the Syndicate text that the upstream rule set.
        var briefingComp = EnsureComp<RoleBriefingComponent>(traitorRole.Value.Owner);
        briefingComp.Briefing = BuildBriefing(ent.Comp, codewords, hasUplink, code, shortVersion: true);

        if (codewords != null && TryComp<RoleCodewordComponent>(traitorRole.Value, out var codewordComp))
            _codeword.SetRoleCodewords((traitorRole.Value, codewordComp), codewords.ToList(), ent.Comp.CodewordColor);
    }

    /// <summary>
    ///     Gives the traitor a remote NT store, opened by a PDA ringer code or, if they have no PDA, an implant.
    /// </summary>
    private bool TryGiveUplink(NanoTraitorRuleComponent comp, EntityUid traitor, EntityUid mindId, out Note[]? code)
    {
        code = null;

        var balance = comp.StartingBalance;
        if (_jobs.MindTryGetJob(mindId, out var job))
            balance = balance < job.AntagAdvantage ? FixedPoint2.Zero : balance - job.AntagAdvantage;

        var store = Spawn(comp.UplinkStore, MapCoordinates.Nullspace);

        if (_uplink.FindUplinkTarget(traitor) != null)
        {
            var ev = new GenerateUplinkCodeEvent();
            RaiseLocalEvent(store, ref ev);
            code = ev.Code;
        }

        // The store has to be owned before implanting, so the implant can find it.
        SetupStore(comp, store, mindId, balance);

        if (code != null)
            return true;

        if (_implant.AddImplant(traitor, comp.UplinkImplant) != null)
            return true;

        Log.Error($"Could not give {ToPrettyString(traitor)} a NanoTrasen uplink.");
        Del(store);
        return false;
    }

    private void SetupStore(NanoTraitorRuleComponent comp, EntityUid store, EntityUid mindId, FixedPoint2 balance)
    {
        var storeComp = EnsureComp<StoreComponent>(store);
        storeComp.AccountOwner = mindId;
        storeComp.Balance.Clear();
        _store.TryAddCurrency(new() { { comp.Currency, balance } }, store, storeComp);

        var initEv = new StoreInitializedEvent(
            TargetUser: mindId,
            Store: store,
            UseDiscounts: comp.GiveDiscounts,
            Listings: _store.GetAvailableListings(mindId, store, storeComp).ToArray());
        RaiseLocalEvent(ref initEv);
    }

    private string BuildBriefing(NanoTraitorRuleComponent comp,
        string[]? codewords,
        bool hasUplink,
        Note[]? code,
        bool shortVersion)
    {
        var sb = new StringBuilder();

        if (!shortVersion)
            sb.AppendLine(Loc.GetString(comp.GreetingLoc));

        if (codewords != null)
        {
            var codewordsLoc = shortVersion ? comp.CodewordsShortLoc : comp.CodewordsLoc;
            sb.AppendLine(Loc.GetString(codewordsLoc, ("codewords", string.Join(", ", codewords))));
        }

        if (hasUplink)
        {
            if (code != null)
            {
                var codeLoc = shortVersion ? comp.UplinkCodeShortLoc : comp.UplinkCodeLoc;
                sb.AppendLine(Loc.GetString(codeLoc, ("code", string.Join("-", code).Replace("sharp", "#"))));
            }
            else
            {
                var implantLoc = shortVersion ? comp.UplinkImplantShortLoc : comp.UplinkImplantLoc;
                sb.AppendLine(Loc.GetString(implantLoc));
            }
        }

        return sb.ToString().TrimEnd();
    }
}
