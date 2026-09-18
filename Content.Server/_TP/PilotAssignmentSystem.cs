using Content.Server._TP.Falling.Components;
using Content.Server.Roles;
using Content.Shared._TP.Mechs.Components;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Verbs;

namespace Content.Server._TP;

public sealed partial class PilotAssignmentSystem : EntitySystem
{
    [Dependency] private IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AppearanceComponent, GetVerbsEvent<ActivationVerb>>(ActivateVerb);
        SubscribeLocalEvent<FallSystemComponent, ExaminedEvent>(OnExamine);
    }


    private void ActivateVerb(EntityUid uid, AppearanceComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!TryComp<StepfatherComponent>(args.User, out var stepfather))
            return;

        if (!stepfather.IsSubverted)
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

     private void ModifyRole(EntityUid uid, EntityUid target, EntityUid user, AppearanceComponent component)
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

        if (!TryComp<AppearanceComponent>(args.Examined, out var target))
            return;

        if (!TryComp<StepfatherComponent>(args.Examiner, out var stepfather))
            return;

        if (!TryComp<ExpedPilotComponent>(args.Examined, out var pilotComp))
        {
          text = "pilot-currently-no";
        }

        args.PushMarkup(Loc.GetString(text));
    }
}
