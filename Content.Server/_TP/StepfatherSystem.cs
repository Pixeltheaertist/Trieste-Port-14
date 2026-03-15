using Content.Server.Power.EntitySystems;
using Content.Server.Silicons.Laws;
using Content.Shared._TP;
using Content.Shared.Alert;
using Content.Shared.Emag.Systems;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._TP;

/// <summary>
///     The system handling the StepfatherComponent.
/// </summary>
public sealed class StepfatherSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SiliconLawSystem _siliconLaw = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StepfatherComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StepfatherComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<StepfatherComponent, MeleeAttackEvent>(OnMeleeAttack);
        SubscribeLocalEvent<StepfatherComponent, GotEmaggedEvent>(OnGotEmagged);
        SubscribeLocalEvent<StepfatherComponent, MechEntryEvent>(OnMechEntry);
        SubscribeLocalEvent<StepfatherComponent, MechExitEvent>(OnMechExit);
    }

    private void OnMechExit(Entity<StepfatherComponent> ent, ref MechExitEvent args)
    {
        UpdateMovementSpeed(ent.Owner);
    }

    private void OnMechEntry(Entity<StepfatherComponent> ent, ref MechEntryEvent args)
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

        if (!TryComp<MechComponent>(ent.Owner, out var mech))
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

        mech.PilotWhitelist = null;
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
        if (TryComp<PowerCellDrawComponent>(ent.Owner, out var drawComp))
        {
            drawComp.DrawRate = 1.0f;
            ent.Comp.ResetDrawTime = 1.0f;
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
        // First we get the MECH battery container.
        // If none is found, we set the base speed and sprint speed to 0.
        if (!_container.TryGetContainer(uid, "mech-battery-slot", out var container) ||
            container.ContainedEntities.Count == 0)
        {
            _movementSpeedModifier.ChangeBaseSpeed(uid, 0f, 0f, 20.0f);
            return;
        }

        // Otherwise if it IS NOT a battery component, we also set the speeds to 0.
        var batteryEntity = container.ContainedEntities[0];
        if (!TryComp<BatteryComponent>(batteryEntity, out var powerCell))
        {
            _movementSpeedModifier.ChangeBaseSpeed(uid, 0f, 0f, 20.0f);
            return;
        }

        // Now we check the charge percentage.
        // If it's low (10%), we then halve the speed.
        // Otherwise, set it to full-speed YML values.
        var chargePercent = powerCell.CurrentCharge / powerCell.MaxCharge;
        if (chargePercent <= 0.1f)
        {
            _movementSpeedModifier.ChangeBaseSpeed(uid, 1.125f, 1.8f, 20f);
        }
        else
        {
            _movementSpeedModifier.ChangeBaseSpeed(uid, 2.25f, 3.6f, 20f);
        }
    }

    private void UpdateBatteryAlert(Entity<StepfatherComponent> ent)
    {
        // First, we get the mech battery slot container.
        // If we don't have a battery, change the hud display to "No Battery".
        if (!_container.TryGetContainer(ent, "mech-battery-slot", out var container) ||
            container.ContainedEntities.Count == 0)
        {
            _alerts.ClearAlert(ent.Owner, ent.Comp.BatteryAlert);
            _alerts.ShowAlert(ent.Owner, ent.Comp.NoBatteryAlert);
            return;
        }

        // Now we set a variable for the first (and only) contained entity.
        // Then check if IT HAS a battery component. If so,
        var batteryEntity = container.ContainedEntities[0];
        if (!TryComp<BatteryComponent>(batteryEntity, out var battery))
        {
            _alerts.ClearAlert(ent.Owner, ent.Comp.BatteryAlert);
            _alerts.ShowAlert(ent.Owner, ent.Comp.NoBatteryAlert);
            return;
        }

        var chargePercent = (short)MathF.Round(battery.CurrentCharge / battery.MaxCharge * 10f);

        if (chargePercent == 0 && battery.CurrentCharge > 0)
            chargePercent = 1;

        _alerts.ClearAlert(ent.Owner, ent.Comp.NoBatteryAlert);
        _alerts.ShowAlert(ent.Owner, ent.Comp.BatteryAlert, chargePercent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StepfatherComponent, PowerCellDrawComponent>();
        while (query.MoveNext(out var stepfatherUid, out var stepfather, out var draw))
        {
            if (stepfather.ResetDrawTime > 0)
            {
                stepfather.ResetDrawTime -= frameTime;

                if (stepfather.ResetDrawTime <= 0)
                    draw.DrawRate = 0.6f;
            }

            stepfather.DrainAccumulator += frameTime;
            if (stepfather.DrainAccumulator >= 1f)
            {
                stepfather.DrainAccumulator -= 1f;

                if (!_container.TryGetContainer(stepfatherUid, "mech-battery-slot", out var container)
                    || container.ContainedEntities.Count == 0)
                {
                    continue;
                }

                var batteryEntity = container.ContainedEntities[0];
                if (TryComp<BatteryComponent>(batteryEntity, out _))
                {
                    _battery.TryUseCharge(batteryEntity, draw.DrawRate);
                }
            }
        }
    }
}
