using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.PDA.Ringer;
using Content.Server.Roles;
using Content.Server.Traitor.Uplink;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.NPC.Systems;
using Content.Shared.PDA;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Roles.RoleCodeword;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using System.Text;
using Content.Shared.Implants;

namespace Content.Server.GameTicking.Rules;

public sealed class TraitorRuleSystem : GameRuleSystem<TraitorRuleComponent>
{
    private static readonly Color TraitorCodewordColor = Color.FromHex("#cc3b3b");

    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRoleCodewordSystem _roleCodewordSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;

    private readonly IEntityManager _entityManager = IoCManager.Resolve<IEntityManager>();

    public override void Initialize()
    {
        base.Initialize();

        Log.Level = LogLevel.Debug;

        SubscribeLocalEvent<TraitorRuleComponent, AfterAntagEntitySelectedEvent>(AfterEntitySelected);
        SubscribeLocalEvent<TraitorRuleComponent, ObjectivesTextPrependEvent>(OnObjectivesTextPrepend);
    }

    protected override void Added(EntityUid uid, TraitorRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        component.SyndicateCodewords = GenerateTraitorCodewords(component, "Syndicate");
        component.NanoTrasenCodewords = GenerateTraitorCodewords(component, "NanoTrasen");

        // Set the shared codewords field for backwards compatibility
        component.Codewords = component.SyndicateCodewords;

        _adminLogger.Add(LogType.EventStarted,
            LogImpact.Low,
            $"Syndicate codewords generated for game rule {ToPrettyString(args.RuleEntity)}: {string.Join(", ", component.SyndicateCodewords)}");
        _adminLogger.Add(LogType.EventStarted,
            LogImpact.Low,
            $"NanoTrasen codewords generated for game rule {ToPrettyString(args.RuleEntity)}: {string.Join(", ", component.NanoTrasenCodewords)}");
    }

    private void AfterEntitySelected(Entity<TraitorRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        Log.Debug($"AfterAntagEntitySelected {ToPrettyString(ent)}");

        var isSyndicate = _random.Prob(0.5f);

        if (isSyndicate)
        {
            MakeTraitor(args.EntityUid, ent);
        }
        else
        {
            MakeNanoTrasenTraitor(args.EntityUid, ent);
        }
    }

    public string[] GenerateTraitorCodewords(TraitorRuleComponent component, String type)
    {
        var adjectives = _prototypeManager.Index(component.CodewordAdjectives).Values;
        var verbs = _prototypeManager.Index(component.CodewordVerbs).Values;
        var codewordPool = adjectives.Concat(verbs).ToList();
        var finalCodewordCount = Math.Min(component.CodewordCount, codewordPool.Count);
        string[] codewords = new string[finalCodewordCount];
        for (var i = 0; i < finalCodewordCount; i++)
        {
            codewords[i] = Loc.GetString(_random.PickAndTake(codewordPool));
        }

        if (type == "Syndicate")
        {
            component.SyndicateCodewords = codewords;
        }
        else
        {
            component.NanoTrasenCodewords = codewords;
        }

        return codewords;
    }

    // Syndicate traitors
    public bool MakeTraitor(EntityUid traitor, TraitorRuleComponent component)
    {
        Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - start");

        //Grab the mind if it wasn't provided
        if (!_mindSystem.TryGetMind(traitor, out var mindId, out var mind))
        {
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)}  - failed, no Mind found");
            return false;
        }

        var briefing = "";

        if (component.GiveCodewords)
        {
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - added codewords flufftext to briefing");
            briefing = Loc.GetString("traitor-role-codewords-short", ("codewords", string.Join(", ", component.SyndicateCodewords)));
        }

        var issuer = _random.Pick(_prototypeManager.Index(component.ObjectiveIssuers));

        // Uplink code will go here if applicable, but we still need the variable if there aren't any
        Note[]? code = null;

        if (component.GiveUplink)
        {
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Uplink start");

            var implantPrototypeId = "UplinkImplant";
            var implantSystem = _entityManager.System<SharedSubdermalImplantSystem>();
            implantSystem.AddImplants(traitor, new HashSet<string> { implantPrototypeId });

            // Calculate the amount of currency on the uplink.
            var startingBalance = component.StartingBalance;
            if (_jobs.MindTryGetJob(mindId, out var prototype))
            {
                if (startingBalance < prototype.AntagAdvantage) // Can't use Math functions on FixedPoint2
                    startingBalance = 0;
                else
                    startingBalance = startingBalance - prototype.AntagAdvantage;
            }

            // Choose and generate an Uplink, and return the uplink code if applicable
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Uplink request start");
            var uplinkParams = RequestUplink(traitor, startingBalance, briefing);
            code = uplinkParams.Item1;
            briefing = uplinkParams.Item2;
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Uplink request completed");
        }

        string[]? codewords = null;
        if (component.GiveCodewords)
        {
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - set codewords from component");
            codewords = component.SyndicateCodewords;
        }

        if (component.GiveBriefing)
        {
            _antag.SendBriefing(traitor, GenerateBriefing(codewords, code, "Syndicate", issuer), null, component.GreetSoundNotification);
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Sent the Briefing");
        }

        Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Adding TraitorMind");
        component.TraitorMinds.Add(mindId);

        // Assign briefing
        //Since this provides neither an antag/job prototype, nor antag status/roletype,
        //and is intrinsically related to the traitor role
        //it does not need to be a separate Mind Role Entity
        _roleSystem.MindHasRole<TraitorRoleComponent>(mindId, out var traitorRole);
        if (traitorRole is not null)
        {
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Add traitor briefing components");
            // Check if the RoleBriefingComponent already exists before adding it
            if (!HasComp<RoleBriefingComponent>(traitorRole.Value.Owner))
                AddComp<RoleBriefingComponent>(traitorRole.Value.Owner);

            // Safe access instead of direct access.
            var briefingComp = _entityManager.GetComponent<RoleBriefingComponent>(traitorRole.Value.Owner);
            briefingComp.Briefing = briefing;
        }
        else
        {
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - did not get traitor briefing");
        }

        // Send codewords to only the traitor client
        var color = TraitorCodewordColor; // Fall back to a dark red Syndicate color if a prototype is not found

        RoleCodewordComponent codewordComp = EnsureComp<RoleCodewordComponent>(mindId);
        _roleCodewordSystem.SetRoleCodewords(codewordComp, "traitor", component.SyndicateCodewords.ToList(), color);

        // Change the faction
        Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Change faction");
        _npcFaction.RemoveFaction(traitor, component.NanoTrasenFaction, false);
        _npcFaction.AddFaction(traitor, component.SyndicateFaction);

        Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Finished");
        return true;
    }

    // NT Traitor
    public bool MakeNanoTrasenTraitor(EntityUid traitor, TraitorRuleComponent component)
    {
        Log.Debug($"MakeNanoTrasenTraitor {ToPrettyString(traitor)} - start");

        //Grab the mind if it wasn't provided
        if (!_mindSystem.TryGetMind(traitor, out var mindId, out var mind))
        {
            Log.Debug($"MakeNanoTrasenTraitor {ToPrettyString(traitor)}  - failed, no Mind found");
            return false;
        }

        var briefing = "";

        if (component.GiveCodewords)
        {
            Log.Debug($"MakeNanoTrasenTraitor {ToPrettyString(traitor)} - added codewords flufftext to briefing");
            briefing = Loc.GetString("traitor-role-codewords-short", ("codewords", string.Join(", ", component.NanoTrasenCodewords)));
        }

        var issuer = _random.Pick(_prototypeManager.Index(component.ObjectiveIssuers));

        // Uplink code will go here if applicable, but we still need the variable if there aren't any
        Note[]? code = null;

        if (component.GiveUplinkNT)
        {
            Log.Debug($"MakeNanoTrasenTraitor {ToPrettyString(traitor)} - Uplink start");
            var implantPrototypeId = "UplinkImplantNT";
            var implantSystem = _entityManager.System<SharedSubdermalImplantSystem>();
            implantSystem.AddImplants(traitor, new HashSet<string> { implantPrototypeId });

            Log.Debug($"MakeNanoTrasenTraitor {ToPrettyString(traitor)} - Uplink request completed");
        }

        string[]? codewords = null;
        if (component.GiveCodewords)
        {
            Log.Debug($"MakeNanoTrasenTraitor {ToPrettyString(traitor)} - set codewords from component");
            codewords = component.NanoTrasenCodewords;
        }

        if (component.GiveBriefing)
        {
            _antag.SendBriefing(traitor, GenerateBriefing(codewords, code, "NanoTrasen", issuer), null, component.GreetSoundNotificationNT);
            Log.Debug($"MakeNanoTrasenTraitor {ToPrettyString(traitor)} - Sent the Briefing");
        }

        Log.Debug($"MakeNanoTrasenTraitor {ToPrettyString(traitor)} - Adding TraitorMind");
        component.TraitorMinds.Add(mindId);

        // Assign briefing
        //Since this provides neither an antag/job prototype, nor antag status/roletype,
        //and is intrinsically related to the traitor role
        //it does not need to be a separate Mind Role Entity
        _roleSystem.MindHasRole<TraitorRoleComponent>(mindId, out var traitorRole);
        if (traitorRole is not null)
        {
            Log.Debug($"MakeNanoTrasenTraitor {ToPrettyString(traitor)} - Add traitor briefing components");
            // Check if the RoleBriefingComponent already exists before adding it
            if (!HasComp<RoleBriefingComponent>(traitorRole.Value.Owner))
                AddComp<RoleBriefingComponent>(traitorRole.Value.Owner);

            var briefingComp = _entityManager.GetComponent<RoleBriefingComponent>(traitorRole.Value.Owner);
            briefingComp.Briefing = briefing;
        }
        else
        {
            Log.Debug($"MakeNanoTrasenTraitor {ToPrettyString(traitor)} - did not get traitor briefing");
        }

        // Send codewords to only the traitor client
        var color = TraitorCodewordColor; // Fall back to a dark red Syndicate color if a prototype is not found

        RoleCodewordComponent codewordComp = EnsureComp<RoleCodewordComponent>(mindId);
        _roleCodewordSystem.SetRoleCodewords(codewordComp, "nanotraitor", component.NanoTrasenCodewords.ToList(), color);

        // Change the faction
        Log.Debug($"MakeNanoTrasenTraitor {ToPrettyString(traitor)} - Change faction");
        _npcFaction.RemoveFaction(traitor, component.NanoTrasenFaction, false);
        _npcFaction.AddFaction(traitor, component.NanoTrasenTraitorFaction);

        Log.Debug($"MakeNanoTrasenTraitor {ToPrettyString(traitor)} - Finished");
        return true;
    }

    private (Note[]?, string) RequestUplink(EntityUid traitor, FixedPoint2 startingBalance, string briefing)
    {
        var pda = _uplink.FindUplinkTarget(traitor);
        Note[]? code = null;

        Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Uplink add");
        var uplinked = _uplink.AddUplink(traitor, startingBalance, pda, true);

        if (pda is not null && uplinked)
        {
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Uplink is PDA");
            // Codes are only generated if the uplink is a PDA
            var ev = new GenerateUplinkCodeEvent();
            RaiseLocalEvent(pda.Value, ref ev);

            if (ev.Code is { } generatedCode)
            {
                code = generatedCode;

                // If giveUplink is false the uplink code part is omitted
                briefing = string.Format("{0}\n{1}",
                    briefing,
                    Loc.GetString("traitor-role-uplink-code-short", ("code", string.Join("-", code).Replace("sharp", "#"))));
                return (code, briefing);
            }
        }
        else if (pda is null && uplinked)
        {
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Uplink is implant");
            briefing += "\n" + Loc.GetString("traitor-role-uplink-implant-short");
        }

        return (null, briefing);
    }

    // TODO: AntagCodewordsComponent
    private void OnObjectivesTextPrepend(EntityUid uid, TraitorRuleComponent comp, ref ObjectivesTextPrependEvent args)
    {
        if(comp.GiveCodewords)
            args.Text += "\n" + Loc.GetString("traitor-round-end-codewords", ("codewords", string.Join(", ", comp.Codewords)));
    }

    // TODO: figure out how to handle this? add priority to briefing event?
    private string GenerateBriefing(string[]? codewords, Note[]? uplinkCode, string faction, string? objectiveIssuer = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine(faction == "NanoTrasen"
            ? Loc.GetString("traitor-role-greeting-nt",
                ("corporation", objectiveIssuer ?? Loc.GetString("objective-issuer-unknown")))
            : Loc.GetString("traitor-role-greeting",
                ("corporation", objectiveIssuer ?? Loc.GetString("objective-issuer-unknown"))));

        if (codewords != null)
        {
            sb.AppendLine(faction == "NanoTrasen"
                ? Loc.GetString("traitor-role-codewords-nt", ("codewords", string.Join(", ", codewords)))
                : Loc.GetString("traitor-role-codewords", ("codewords", string.Join(", ", codewords))));
        }

        if (uplinkCode != null)
        {
            sb.AppendLine(faction == "NanoTrasen"
                ? Loc.GetString("traitor-role-uplink-code-nt",
                    ("code", string.Join("-", uplinkCode).Replace("sharp", "#")))
                : Loc.GetString("traitor-role-uplink-code",
                    ("code", string.Join("-", uplinkCode).Replace("sharp", "#"))));
        }
        else
        {
            sb.AppendLine(Loc.GetString("traitor-role-uplink-implant"));
        }


        return sb.ToString();
    }

    public List<(EntityUid Id, MindComponent Mind)> GetOtherTraitorMindsAliveAndConnected(MindComponent ourMind)
    {
        List<(EntityUid Id, MindComponent Mind)> allTraitors = new();

        var query = EntityQueryEnumerator<TraitorRuleComponent>();
        while (query.MoveNext(out var uid, out var traitor))
        {
            foreach (var role in GetOtherTraitorMindsAliveAndConnected(ourMind, (uid, traitor)))
            {
                if (!allTraitors.Contains(role))
                    allTraitors.Add(role);
            }
        }

        return allTraitors;
    }

    private List<(EntityUid Id, MindComponent Mind)> GetOtherTraitorMindsAliveAndConnected(MindComponent ourMind, Entity<TraitorRuleComponent> rule)
    {
        var traitors = new List<(EntityUid Id, MindComponent Mind)>();
        foreach (var mind in _antag.GetAntagMinds(rule.Owner))
        {
            if (mind.Comp == ourMind)
                continue;

            traitors.Add((mind, mind));
        }

        return traitors;
    }
}
