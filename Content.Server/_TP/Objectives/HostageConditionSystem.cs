using Content.Server.Objectives.Systems;
using Content.Shared.Cuffs;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server._TP.Objectives;

public sealed partial class HostageConditionSystem : EntitySystem
{
    [Dependency] private SharedCuffableSystem _cuffable = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TargetObjectiveSystem _target = default!;

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<HostageConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(ent, out var target))
            return;

        args.Progress = IsHeldHostage(ent.Comp, args.Mind, target.Value) ? 1f : 0f;
    }

    private bool IsHeldHostage(HostageConditionComponent comp, MindComponent captorMind, EntityUid targetMindId)
    {
        if (!TryComp<MindComponent>(targetMindId, out var targetMind)
            || _mind.IsCharacterDeadIc(targetMind)
            || targetMind.OwnedEntity is not { } hostage)
            return false;

        if (captorMind.OwnedEntity is not { } captor)
            return false;

        if (!_cuffable.IsCuffed(hostage))
            return false;

        // InRange compares map positions, so a hostage stuffed in a locker or bag next to you still counts.
        return _transform.InRange(captor, hostage, comp.Range);
    }
}
