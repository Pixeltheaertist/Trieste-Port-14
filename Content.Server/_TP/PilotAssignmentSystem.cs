using Robust.Shared.Utility;
using Content.Server.Popups;
using Content.Server.Xenoarchaeology.Equipment.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.Interaction;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Content.Server.Paper;
using Robust.Server.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Examine;
using Content.Shared.Plankton;
using System.Linq;
using Content.Server.Falling;
using Content.Shared.Paper;
using Content.Shared.Humanoid;
using Content.Server.Roles;

namespace Content.Server.TP.Systems;

public sealed class PilotAssignmentSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HumanoidAppearanceComponent, GetVerbsEvent<ActivationVerb>>(ActivateVerb);
        SubscribeLocalEvent<FallSystemComponent, ExaminedEvent>(OnExamine);
    }


    private void ActivateVerb(EntityUid uid, HumanoidAppearanceComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!TryComp<StepfatherComponent>(args.User, out var stepfather))
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

        if (!TryComp<StepfatherComponent>(args.Examiner, out var stepfather))
            return;

        if (!TryComp<ExpedPilotComponent>(args.Examined, out var pilotComp))
        {
          text = "pilot-currently-no";
        }

        args.PushMarkup(Loc.GetString(text));
    }
}
