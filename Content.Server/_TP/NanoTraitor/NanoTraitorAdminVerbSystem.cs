using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Antag;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._TP.NanoTraitor;

/// <summary>
///     Adds a "Make NanoTraitor" admin antag verb.
/// </summary>
public sealed partial class NanoTraitorAdminVerbSystem : EntitySystem
{
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;

    private static readonly EntProtoId NanoTraitorRule = "TP14TraitorRuleNanoTraitor";
    private static readonly ProtoId<AntagSpecifierPrototype> NanoTraitorSpec = "TP14AntagSpecNanoTraitor";

    [SubscribeLocalEvent]
    private void OnGetVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor)
            || !_adminManager.HasAdminFlag(actor.PlayerSession, AdminFlags.Fun))
            return;

        if (!HasComp<MindContainerComponent>(args.Target) || !TryComp<ActorComponent>(args.Target, out var targetActor))
            return;

        var targetPlayer = targetActor.PlayerSession;
        var name = Loc.GetString("admin-verb-text-make-nanotraitor");
        args.Verbs.Add(new Verb
        {
            Text = name,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Misc/job_icons.rsi"), "Nanotrasen"),
            Act = () => _antag.ForceMakeAntag<NanoTraitorRuleComponent>(targetPlayer, NanoTraitorRule, NanoTraitorSpec),
            Impact = LogImpact.High,
            Message = string.Join(": ", name, Loc.GetString("admin-verb-make-nanotraitor")),
        });
    }
}
