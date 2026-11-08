using Content.Shared._TP.Mechs.Components;
using Content.Shared.Vehicle;
using Content.Shared.Whitelist;

namespace Content.Shared._TP.Mechs.Systems;

/// <summary>
///     Restricts who can climb into the Rotund using <see cref="StepfatherComponent.PilotWhitelist"/>.
///     This is separate from VehicleComponent.OperatorWhitelist so that emagging can clear it.
/// </summary>
public sealed partial class StepfatherEntrySystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StepfatherComponent, ContainerVehicleEntryAttemptEvent>(OnEntryAttempt);
    }

    private void OnEntryAttempt(Entity<StepfatherComponent> ent, ref ContainerVehicleEntryAttemptEvent args)
    {
        if (_whitelist.IsWhitelistFail(ent.Comp.PilotWhitelist, args.Entering))
            args.Cancelled = true;
    }
}
