using Content.Server.DoAfter;
using Content.Shared._TP;
using Content.Shared._TP.Ladder;
using Content.Shared.Climbing.Events;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Verbs;

namespace Content.Server._TP.Ladder;

/// <summary>
///     A class to handle Ladders.
/// </summary>
public sealed class LadderSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LadderComponent, ClimbedOnEvent>(OnClimbed);
        SubscribeLocalEvent<LadderComponent, ActivateInWorldEvent>(OnActivatedInWorld);
        SubscribeLocalEvent<LadderComponent, GetVerbsEvent<ActivationVerb>>(OnActivationVerb);
        SubscribeLocalEvent<LadderComponent, LadderDoAfterEvent>(OnLadderDoAfter);
    }

    /// <summary>
    ///     A method to handle a custom 'after' event.
    /// </summary>
    /// <param name="ladderUid">LadderComponent Uid</param>
    /// <param name="ladderComp">Ladder Component</param>
    /// <param name="args">LadderDoAfterEvent Arguments</param>
    private void OnLadderDoAfter(EntityUid ladderUid, LadderComponent ladderComp, ref LadderDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        HandleClimbWithoutTimer(ladderComp, args.User);
    }

    /// <summary>
    ///     A method to handle the Verbs menu.
    /// </summary>
    /// <param name="ladderUid">LadderComponent Uid</param>
    /// <param name="ladderComp">Ladder Component</param>
    /// <param name="args">GetVerbsEvent Arguments</param>
    private void OnActivationVerb(EntityUid ladderUid, LadderComponent ladderComp, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        var verb = new ActivationVerb()
        {
            Act = () => HandleClimb(ladderUid, user),
            Text = Loc.GetString("climb-verb-text"),
            Message = Loc.GetString("climb-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     Called when activated. This is for borgs.
    /// </summary>
    /// <param name="ladderUid">LadderComponent Uid</param>
    /// <param name="ladderComp">Ladder Component</param>
    /// <param name="args">ActivateInWorldEvent Arguments</param>
    private void OnActivatedInWorld(EntityUid ladderUid, LadderComponent ladderComp, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<BorgChassisComponent>(args.User) || !HasComp<StepfatherComponent>(args.User))
            return;

        HandleClimb(ladderUid, args.User);

        args.Handled = true;
    }

    /// <summary>
    ///     Called when climbing down a ladder.
    /// </summary>
    /// <param name="ladderUid">Ladder Uid</param>
    /// <param name="ladderComp">Ladder Comp</param>
    /// <param name="args">ClimbedOnEvent Arguments</param>
    private void OnClimbed(EntityUid ladderUid, LadderComponent ladderComp, ref ClimbedOnEvent args)
    {
        if (!HasComp<HandsComponent>(args.Climber))
            return;

        HandleClimbWithoutTimer(ladderComp, args.Climber);
    }

    /// <summary>
    ///     A method to handle climbing the ladder after five seconds.
    /// </summary>
    /// <param name="ladderUid">LadderComponent Uid</param>
    /// <param name="user">User Uid</param>
    private void HandleClimb(EntityUid ladderUid, EntityUid user)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(1.5), new LadderDoAfterEvent(), ladderUid, ladderUid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    /// <summary>
    ///     A method to handle climbing the ladder WITHOUT a timer.
    /// </summary>
    /// <param name="ladderComp">Ladder Component</param>
    /// <param name="userUid">User Uid</param>
    private void HandleClimbWithoutTimer(LadderComponent ladderComp, EntityUid userUid)
    {
        var query = EntityManager.EntityQueryEnumerator<LadderComponent>();
        while (query.MoveNext(out var destUid, out var destComp))
        {
            if (destComp.ThisSide != ladderComp.TargetSide)
                continue;

            _transform.SetCoordinates(userUid, Transform(destUid).Coordinates);
            break;
        }
    }
}
