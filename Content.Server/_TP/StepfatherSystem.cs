using Content.Server.Power.EntitySystems;
using Content.Server.Silicons.Laws;
using Content.Shared._TP.Mechs.Components;
using Content.Shared.Alert;
using Content.Shared.Emag.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Vehicle;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._TP;

/// <summary>
///     The system handling the StepfatherComponent.
/// </summary>
public sealed partial class StepfatherSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private BatterySystem _battery = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SiliconLawSystem _siliconLaw = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StepfatherComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StepfatherComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<StepfatherComponent, MeleeAttackEvent>(OnMeleeAttack);
        SubscribeLocalEvent<StepfatherComponent, GotEmaggedEvent>(OnGotEmagged);
        SubscribeLocalEvent<StepfatherComponent, VehicleOperatorSetEvent>(OnOperatorSet);
    }

    /// <summary>
    ///     Raised when a pilot enters or exits the Rotund.
    /// </summary>
    private void OnOperatorSet(Entity<StepfatherComponent> ent, ref VehicleOperatorSetEvent args)
    {
        UpdateMovementSpeed(ent.Owner);
    }

    private readonly SoundPathSpecifier _soundSpecifier = new("/Audio/Misc/cryo_warning.ogg");

    /// <summary>
    ///     Server-side call for when the Rotund gets emagged.
    ///     I hate protoID code. - Cookie (FatherCheese)
    /// </summary>
    /// <param name="ent">Stepfather Entity</param>
    /// <param name="args">GotEmaggedEvent arguments</param>
    private void OnGotEmagged(Entity<StepfatherComponent> ent, ref GotEmaggedEvent args)
    {
        if (ent.Comp.IsSubverted)
            return;

        var protoLawset = new ProtoId<SiliconLawPrototype>("MechLawsSubverted");
        if (!_proto.TryIndex<SiliconLawsetPrototype>(protoLawset, out var newLawset))
            return;

        var laws = new List<SiliconLaw>();
        foreach (var lawProtoId in newLawset.Laws)
        {
            if (_proto.TryIndex(lawProtoId, out var lawProto))
            {
                laws.Add(new SiliconLaw
                {
                    LawString = Loc.GetString(lawProto.LawString),
                    Order = lawProto.Order,
                });
            }
        }

        ent.Comp.PilotWhitelist = null;
        ent.Comp.IsSubverted = true;
        _siliconLaw.SetLaws(laws, ent.Owner, _soundSpecifier);

        args.Handled = true;
    }

    /// <summary>
    ///    Raised when the Rotund entity attacks with a melee weapon.
    ///    This is used to drain extra power.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnMeleeAttack(Entity<StepfatherComponent> ent, ref MeleeAttackEvent args)
    {
        // First we set a variable; if the weapon IS the rotund, or the drill, then we set it to true.
        var drainingWeapon = args.Weapon == ent.Owner || MetaData(args.Weapon).EntityName == "rotund drill";
        if (!drainingWeapon)
            return;

        // Then we check if the battery exists.
        // If it does, then we set the draw rate from 0.6 to 1, and the draw time from 0 to 2.
        if (TryComp<PowerCellDrawComponent>(ent.Owner, out _))
        {
            _powerCell.TryUseCharge(ent.Owner, 0.5f);
        }
    }

    private void OnPowerCellChanged(EntityUid uid, StepfatherComponent stepFatherComp, PowerCellChangedEvent args)
    {
        UpdateBatteryAlert((uid, stepFatherComp));
        UpdateMovementSpeed(uid);
    }

    private void OnMapInit(EntityUid uid, StepfatherComponent stepfatherComp, MapInitEvent args)
    {
        UpdateBatteryAlert((uid, stepfatherComp));
        UpdateMovementSpeed(uid);

        if (TryComp<InputMoverComponent>(uid, out var mover))
            mover.CanMove = true;
    }

    private void UpdateMovementSpeed(EntityUid uid)
    {
        // No battery means no movement.
        // If it's low (10%), we then halve the speed.
        // Otherwise, set it to full-speed YML values.
        var (walk, sprint) = TryGetChargeLevel(uid, out var chargeLevel)
            ? chargeLevel <= 0.1f ? (1.125f, 1.8f) : (2.25f, 3.6f)
            : (0f, 0f);

        // This runs every second, so only dirty the speed when the tier actually changes.
        if (TryComp<MovementSpeedModifierComponent>(uid, out var speed)
            && MathHelper.CloseTo(speed.BaseWalkSpeed, walk)
            && MathHelper.CloseTo(speed.BaseSprintSpeed, sprint))
            return;

        _movementSpeedModifier.ChangeBaseSpeed(uid, walk, sprint, 20f);
    }

    /// <summary>
    ///     Gets the current charge level (0-1) of the Rotund's core battery.
    ///     The PowerCellSlot points at the mech battery slot, so this is the only battery it has.
    /// </summary>
    private bool TryGetChargeLevel(EntityUid uid, out float chargeLevel)
    {
        chargeLevel = 0f;
        if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery))
            return false;

        chargeLevel = _battery.GetChargeLevel(battery.Value.AsNullable());
        return true;
    }

    private void UpdateBatteryAlert(Entity<StepfatherComponent> ent)
    {
        // If we don't have a battery, change the hud display to "No Battery".
        if (!TryGetChargeLevel(ent.Owner, out var chargeLevel))
        {
            _alerts.ClearAlert(ent.Owner, ent.Comp.BatteryAlert);
            _alerts.ShowAlert(ent.Owner, ent.Comp.NoBatteryAlert);
            return;
        }

        var chargePercent = (short)MathF.Round(chargeLevel * 10f);

        if (chargePercent == 0 && chargeLevel > 0)
            chargePercent = 1;

        _alerts.ClearAlert(ent.Owner, ent.Comp.NoBatteryAlert);
        _alerts.ShowAlert(ent.Owner, ent.Comp.BatteryAlert, chargePercent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // PowerCellDraw handles the passive drain, so we just keep speed and the battery alert in sync with it.
        var query = EntityQueryEnumerator<StepfatherComponent>();
        while (query.MoveNext(out var stepfatherUid, out var stepfather))
        {
            stepfather.DrainAccumulator += frameTime;
            if (stepfather.DrainAccumulator < 1f)
                continue;

            stepfather.DrainAccumulator -= 1f;
            UpdateBatteryAlert((stepfatherUid, stepfather));
            UpdateMovementSpeed(stepfatherUid);
        }
    }
}
