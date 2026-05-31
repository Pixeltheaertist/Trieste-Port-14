using Robust.Shared.Containers;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;

using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Robust.Shared.Timing;
using Content.Shared.FixedPoint;

namespace Content.Server._TP.Oil;
public sealed class OilProductionSystem : EntitySystem
{
    //Somehow have to make this shit produce oil
    //Wish me luck
    // of corse there was a easy way to do it, only found after like 8 hours of suffering

    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<OilProductionComponent, EntInsertedIntoContainerMessage>(ContainerInsert);
        SubscribeLocalEvent<OilProductionComponent, EntRemovedFromContainerMessage>(ContainerRemoved);
    }
    private void ContainerInsert(EntityUid uid, OilProductionComponent component, ref EntInsertedIntoContainerMessage args)
    {
        component.ValidContainer = true;
    }

    private void ContainerRemoved(EntityUid uid, OilProductionComponent component, ref EntRemovedFromContainerMessage args)
    {
        component.ValidContainer = false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<OilProductionComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var regen, out var manager))
        {
            if (_timing.CurTime < regen.NextRegenTime)
                continue;

            if (_power.IsPowered(uid))
            {
                // timer ignores if its full, it's just a fixed cycle
                regen.NextRegenTime = _timing.CurTime + regen.Duration;
                if (_solutionContainer.ResolveSolution((uid, manager), regen.SolutionName, ref regen.SolutionRef, out var solution))
                {
                    var amount = FixedPoint2.Min(solution.AvailableVolume, regen.Generated.Volume);
                    if (amount <= FixedPoint2.Zero)
                        continue;

                    // dont bother cloning and splitting if adding the whole thing
                    Solution generated;
                    if (amount == regen.Generated.Volume)
                    {
                        generated = regen.Generated;
                    }
                    else
                    {
                        generated = regen.Generated.Clone().SplitSolution(amount);
                    }

                    _solutionContainer.TryAddSolution(regen.SolutionRef.Value, generated);
                }
            }
        }
    }
}

