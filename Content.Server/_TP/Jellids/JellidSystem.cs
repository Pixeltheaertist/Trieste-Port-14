using Content.Server.Atmos.EntitySystems;
using Content.Shared._TP.Jellids;
using Content.Shared.Alert;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Electrocution;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Medical;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Smoking;
using Content.Shared.Tag;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._TP.Jellids;

public sealed partial class JellidSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private SharedTemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Proper charging.
        SubscribeLocalEvent<BatteryComponent, UseInHandEvent>(OnUseBatteryInHand);
        SubscribeLocalEvent<JellidComponent, JellidBatteryDoAfterEvent>(OnJellidDoAfter);
        SubscribeLocalEvent<JellidComponent, ChargeChangedEvent>(OnChargeChanged);

        // Electrocution events.
        SubscribeLocalEvent<JellidComponent, ElectrocutedEvent>(OnElectrocution);
        SubscribeLocalEvent<JellidComponent, TargetBeforeDefibrillatorZapsEvent>(OnBeforeZapped);
    }

    private void OnChargeChanged(Entity<JellidComponent> ent, ref ChargeChangedEvent args)
    {
        if (!TryComp<BatteryComponent>(ent.Owner, out var batteryComp))
            return;

        var currentCharge = _battery.GetCharge((ent.Owner, batteryComp));
        var chargeLevel = (short)MathF.Round(_battery.GetChargeLevel((ent.Owner, batteryComp)) * 10f);

        // Battery alert stuff.
        if (currentCharge > batteryComp.MaxCharge * 0.1)
        {
            _alerts.ClearAlert(ent.Owner, ent.Comp.NoBatteryAlert);
            _alerts.ShowAlert(ent.Owner, ent.Comp.BatteryAlert, chargeLevel);
        }
        else
        {
            _alerts.ClearAlert(ent.Owner, ent.Comp.BatteryAlert);
            _alerts.ShowAlert(ent.Owner, ent.Comp.NoBatteryAlert);
        }

        // Damage jellids below the damage start value.
        if (currentCharge <= batteryComp.MaxCharge * 0.1)
        {
            var isCharging = currentCharge > batteryComp.LastCharge;
            if (isCharging)
                return;

            var damage = new DamageSpecifier
            {
                DamageDict = { ["Slash"] = 2f }
            };
            _damageable.TryChangeDamage(ent.Owner, damage, origin: ent.Owner);
        }
    }

    /// <summary>
    ///     A raised-event method that will charge jellids upon being defibbed.
    /// </summary>
    /// <param name="ent">Jellid entity</param>
    /// <param name="args">TargetBeforeDefibrillatorZapsEvent arguments</param>
    private void OnBeforeZapped(Entity<JellidComponent> ent, ref TargetBeforeDefibrillatorZapsEvent args)
    {
        if (args.DefibTarget != ent.Owner)
            return;

        // If the target has a battery (Jellids), restores some of their internal energy.
        // This will heal Jellids and prevent instantly dying again.
        if (!HasComp<BatteryComponent>(ent.Owner))
            return;

        _battery.ChangeCharge(ent.Owner, ent.Comp.ZapCharge);
    }

    /// <summary>
    /// A raised-event method that will charge jellids upon being electrocuted.
    /// </summary>
    /// <param name="ent">Jellid entity</param>
    /// <param name="args">ElectrocutedEvent args</param>
    private void OnElectrocution(Entity<JellidComponent> ent, ref ElectrocutedEvent args)
    {
        if (!HasComp<BatteryComponent>(ent.Owner))
            return;

        // This *should* be scaled to the power level, with a max of 200. (or 1.0)
        _battery.ChangeCharge(ent.Owner, ent.Comp.ZapCharge * args.SiemensCoefficient);
    }

    /// <summary>
    ///     What the do-after triggers when the user uses the battery.
    ///     This handles the battery power transfer.
    /// </summary>
    /// <param name="ent">JellidComponent Entity</param>
    /// <param name="args">JellidBatteryDoAfterEvent Arguments</param>
    private void OnJellidDoAfter(Entity<JellidComponent> ent, ref JellidBatteryDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp<BatteryComponent>(args.Used, out var batteryComp))
            return;

        if (!TryComp<BatteryComponent>(args.User, out var jellidBatteryComp))
            return;

        if (!TryComp<JellidComponent>(args.User, out var jellidComp))
            return;

        var drain = batteryComp.MaxCharge * jellidComp.DrainPercent;

        var itemCharge = _battery.GetCharge((args.Used.Value, batteryComp));
        if (itemCharge < drain)
        {
            _popup.PopupEntity(Loc.GetString("jellid-used-failed"), args.User, args.User);
            args.Repeat = false;
            return;
        }

        var jellidCharge = _battery.GetCharge((args.User, jellidBatteryComp));
        if (jellidCharge + drain >= jellidBatteryComp.MaxCharge)
        {
            args.Repeat = false;
            return;
        }

        _battery.ChangeCharge(args.Used.Value, -drain);
        _battery.ChangeCharge(args.User, drain);

        _audio.PlayPvs(jellidComp.BatteryUseSound, ent.Owner, AudioParams.Default.WithLoop(false).WithVolume(-3));
        _popup.PopupEntity(Loc.GetString("jellid-used-success"), args.User, args.User);

        args.Repeat = jellidCharge + drain <= jellidBatteryComp.MaxCharge;
        args.Handled = true;
    }

    private void OnUseBatteryInHand(Entity<BatteryComponent> ent, ref UseInHandEvent args)
    {
        if (!TryComp<JellidComponent>(args.User, out _))
            return;

        // Check for fireproof gloves before charging. They also block charging, as requested by Pix.
        if (_inventory.TryGetSlotEntity(args.User, "gloves", out var glovesUid)
            && _tag.HasTag(glovesUid.Value, FireproofTag))
            return;

        var doAfter = new DoAfterArgs(EntityManager, args.User, 1f, new JellidBatteryDoAfterEvent(), args.User, null, ent.Owner)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
            NeedHand = true,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    // The jellid-proof gloves tag proto ID.
    private static readonly ProtoId<TagPrototype> FireproofTag = "PreventsFire";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<JellidComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Timing check so this doesn't run every tick.
            if (_timing.CurTime < comp.NextPowerDrain)
                continue;

            comp.NextPowerDrain = _timing.CurTime + TimeSpan.FromSeconds(1f);

            // Held item check!
            // If the user DOES NOT have gloves on and a battery is held, it will slowly drain into the Jellid.
            // Also check if the user has BURNABLE ITEMS in their hands. If so, burn it to ash.
            var hasFireproofGloves = _inventory.TryGetSlotEntity(uid, "gloves", out var glovesUid)
                                     && _tag.HasTag(glovesUid.Value, FireproofTag);

            if (!hasFireproofGloves)
            {
                UpdateHeldBatteries(uid, comp);
                UpdateHeldBurnables(uid);
                UpdateHeldThermals(uid, comp, frameTime);
            }
        }
    }

    /// <summary>
    ///     Helper method to heat-up solutions being held.
    /// </summary>
    /// <param name="uid">Jellid uid</param>
    /// <param name="jellidComp">Jellid component</param>
    /// <param name="frameTime">FrameTime</param>
    private void UpdateHeldThermals(EntityUid uid, JellidComponent jellidComp, float frameTime)
    {
        if (_hands.GetActiveItem(uid) is not { } heldItem)
            return;

        var energy = jellidComp.HeatTransfer * frameTime;
        if (HasComp<SolutionComponent>(heldItem))
        {
            foreach (var (_, solutionEnt) in _solutionContainer.EnumerateSolutions(heldItem))
            {
                _solutionContainer.AddThermalEnergy(solutionEnt, energy);
            }
        }

        if (TryComp<TemperatureComponent>(heldItem, out var heldTempComp))
            _temperature.ChangeHeat(heldItem, energy, false, heldTempComp);
    }

    private void UpdateHeldBurnables(EntityUid uid)
    {
        if (_hands.GetActiveItem(uid) is not { } heldItem)
            return;

        if (!TryComp<FlammableComponent>(heldItem, out var flammable))
            return;

        if (HasComp<BurningComponent>(heldItem))
            return;

        _flammable.AdjustFireStacks(heldItem, flammable.FireStacks, flammable);
        if (flammable.FireStacks >= 0)
            _flammable.Ignite(heldItem, heldItem, flammable, uid);
    }


    /// <summary>
    ///     Helper method to drain HELD batteries passively into the player.
    ///     This is separate from 'eating' power, but it still feeds them.
    /// </summary>
    /// <param name="uid">Jellid's UID</param>
    /// <param name="jellidComp"></param>
    private void UpdateHeldBatteries(EntityUid uid, JellidComponent jellidComp)
    {
        foreach (var hand in _hands.EnumerateHands(uid))
        {
            if (!HasComp<BatteryComponent>(uid))
                continue;

            if (!_hands.TryGetHeldItem(uid, hand, out var heldItem))
                continue;

            if (!TryComp<BatteryComponent>(heldItem, out var batteryComp))
                continue;

            // Drain at a rate of a constant 2.5 power.
            _battery.ChangeCharge(heldItem.Value, -jellidComp.HeldPassiveDrain);
            _battery.ChangeCharge(uid, jellidComp.HeldPassiveDrain);
        }
    }
}
