using Content.Server.PowerCell;
using Content.Shared._TP;
using Content.Shared.Alert;
using Content.Shared.Movement.Systems;
using Content.Shared.PowerCell.Components;

namespace Content.Server._TP;

/// <summary>
///     The system handling the StepfatherComponent.
/// </summary>
public sealed class StepfatherSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StepfatherComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StepfatherComponent, PowerCellChangedEvent>(OnPowerCellChanged);
    }

    private void OnPowerCellChanged(EntityUid uid, StepfatherComponent component, PowerCellChangedEvent args)
    {
        UpdateBatteryAlert((uid, component));
    }

    private void OnMapInit(EntityUid uid, StepfatherComponent stepfatherComp, MapInitEvent args)
    {
        UpdateBatteryAlert((uid, stepfatherComp));
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void UpdateBatteryAlert(Entity<StepfatherComponent> ent, PowerCellSlotComponent? slotComp = null)
    {
        // Check if the battery is present. If not, then show the no battery alert.
        if (!_powerCell.TryGetBatteryFromSlot(ent, out var battery, slotComp))
        {
            _alerts.ClearAlert(ent.Owner, ent.Comp.BatteryAlert);
            _alerts.ShowAlert(ent.Owner, ent.Comp.NoBatteryAlert);
            return;
        }

        var chargePercent = (short) MathF.Round(battery.CurrentCharge / battery.MaxCharge * 10f);

        // We make sure 0 only shows if they have absolutely no battery.
        // We also account for floating point imprecision.
        if (chargePercent == 0 && _powerCell.HasDrawCharge(ent, cell: slotComp))
            chargePercent = 1;

        _alerts.ClearAlert(ent.Owner, ent.Comp.NoBatteryAlert);
        _alerts.ShowAlert(ent.Owner, ent.Comp.BatteryAlert, chargePercent);
    }
}
