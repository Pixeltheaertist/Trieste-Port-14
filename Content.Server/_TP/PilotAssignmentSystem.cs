using Content.Server._TP.Falling.Components;
using Content.Server.Roles;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Verbs;

namespace Content.Server._TP;

public sealed class PilotAssignmentSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HumanoidAppearanceComponent, GetVerbsEvent<ActivationVerb>>(ActivateVerb);
        SubscribeLocalEvent<FallSystemComponent, ExaminedEvent>(OnExamine);
    }


    private void ActivateVerb(EntityUid uid, HumanoidAppearanceComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!TryComp<Shared._TP.StepfatherComponent>(args.User, out var stepfather))
            return;

        var verb = new ActivationVerb()
        {
            Act = () =>
            {
                ModifyRole(uid, args.Target, args.User, component);
            },
            Text = Loc.GetString("pilot-assignment-switch")
        };

        args.Verbs.Add(verb);
    }

     private void ModifyRole(EntityUid uid, EntityUid target, EntityUid user, HumanoidAppearanceComponent component)
     {
        if (TryComp<ExpedPilotComponent>(target, out var pilotComp))
        {
            _entityManager.RemoveComponent<ExpedPilotComponent>(target);
        }
        else
        {
            EnsureComp<ExpedPilotComponent>(target);
        }
     }

      private void OnExamine(EntityUid uid, FallSystemComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var text = "pilot-currently-yes";

        if (!TryComp<HumanoidAppearanceComponent>(args.Examined, out var target))
            return;

        if (!TryComp<Shared._TP.StepfatherComponent>(args.Examiner, out var stepfather))
            return;

        if (!TryComp<ExpedPilotComponent>(args.Examined, out var pilotComp))
        {
          text = "pilot-currently-no";
        }

        args.PushMarkup(Loc.GetString(text));
    }
}
