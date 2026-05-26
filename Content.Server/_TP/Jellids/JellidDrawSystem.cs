using Content.Server.DoAfter;
using Content.Server.Power.EntitySystems;
using Content.Shared._TP.Jellids;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Electrocution;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._TP.Jellids;

public sealed class JellidDrawSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<TagPrototype> FireproofTag = "PreventsFire";

    // Track the previous charge to detect if this Jellid is charging.
    private readonly Dictionary<EntityUid, float> _previousCharges = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JellidComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<JellidComponent, ElectrocutedEvent>(OnElectrocution);

        SubscribeLocalEvent<BatteryComponent, UseInHandEvent>(OnUseBatteryInHand);
        SubscribeLocalEvent<JellidComponent, JellidBatteryDoAfterEvent>(OnJellidDoAfter);
    }

    /// <summary>
    ///     What the do-after triggers when the user uses the battery.
    ///     This handles the battery power transfer.
    /// </summary>
    /// <param name="ent">JellidComponent Entity</param>
    /// <param name="args">JellidBatteryDoAfterEvent Arguments</param>
    private void OnJellidDoAfter(Entity<JellidComponent> ent, ref JellidBatteryDoAfterEvent args)
    {
        // If the do-after is canceled, or the user has already used the battery, return.
        if (args.Cancelled || args.Handled)
            return;

        // If the held item and user aren't a battery, and the user is NOT a Jellid, return.
        if (!TryComp<BatteryComponent>(args.Used, out var batteryComp))
            return;

        if (!TryComp<JellidComponent>(args.User, out var jellidComp))
            return;

        if (!TryComp<BatteryComponent>(args.User, out var jellidBatteryComp))
            return;

        // Now get the battery's max charge and multiply it by the JellidComponent drain percent.
        // If the battery's current charge is less than the drain, return with a popup.
        // Otherwise, drain the battery and add the charge to the Jellid's battery.
        var drain = batteryComp.MaxCharge * jellidComp.DrainPercent;
        if (batteryComp.CurrentCharge < drain)
        {
            _popup.PopupEntity(Loc.GetString("jellid-used-failed"), args.User, args.User);
            args.Repeat = false;

            return;
        }

        if (jellidBatteryComp.CurrentCharge + drain >= jellidBatteryComp.MaxCharge)
        {
            args.Repeat = false;
            return;
        }

        _battery.SetCharge(args.Used.Value, batteryComp.CurrentCharge - drain);
        _battery.SetCharge(args.User, jellidBatteryComp.CurrentCharge + drain);

        _audio.PlayPvs(jellidComp.BatteryUseSound, ent.Owner, AudioParams.Default.WithLoop(false).WithVolume(-3));
        _popup.PopupEntity(Loc.GetString("jellid-used-success"), args.User, args.User);

        args.Repeat = batteryComp.CurrentCharge >= drain;
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

    private void OnElectrocution(Entity<JellidComponent> ent, ref ElectrocutedEvent args)
    {
        if (!TryComp<BatteryComponent>(ent.Owner, out var battery))
            return;

        var chargeGain = 100f * args.SiemensCoefficient;
        _battery.SetCharge(ent.Owner, Math.Min(battery.CurrentCharge + chargeGain, battery.MaxCharge));
    }

    private void OnShutdown(Entity<JellidComponent> ent, ref ComponentShutdown args)
    {
        _previousCharges.Remove(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<JellidComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.NextPowerDrain)
                continue;

            comp.NextPowerDrain = _timing.CurTime + TimeSpan.FromSeconds(1f);

            if (!TryComp<BatteryComponent>(uid, out var jellidBattery))
                return;

            const float alertChange = 300f;
            var chargePercent = (short) MathF.Round(jellidBattery.CurrentCharge / jellidBattery.MaxCharge * 10f);
            if (jellidBattery.CurrentCharge > alertChange)
            {
                _alerts.ClearAlert(uid, comp.NoBatteryAlert);
                _alerts.ShowAlert(uid, comp.BatteryAlert, chargePercent);
            }
            else
            {
                _alerts.ClearAlert(uid, comp.BatteryAlert);
                _alerts.ShowAlert(uid, comp.NoBatteryAlert);
            }

            const float damageCharge = 20f;
            if (jellidBattery.CurrentCharge >= damageCharge)
            {
                _previousCharges[uid] = jellidBattery.CurrentCharge;
                return;
            }

            // Only damage if the Jellid is NOT charging
            var isCharging = _previousCharges.TryGetValue(uid, out var prevCharge) && jellidBattery.CurrentCharge > prevCharge;

            if (isCharging)
                return;

            var damage = new DamageSpecifier
            {
                DamageDict = { ["Slash"] = 0.1f }
            };
            _damageable.TryChangeDamage(uid, damage, origin: uid);
        }
    }
}
