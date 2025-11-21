using Content.Server.Power.EntitySystems;
using Content.Shared._TP.Jellids;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._TP.Jellids;

public sealed class JellidDrawSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> FireproofTag = "PreventsFire";

    // Track the previous charge to detect if the Jellid is charging.
    private Dictionary<EntityUid, float> _previousCharges = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JellidComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<JellidComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<JellidComponent> ent, ref ComponentShutdown args)
    {
        _previousCharges.Remove(ent);
    }

    /// <summary>
    ///     Called when the battery charge changes.
    ///     Also displays an alert if the battery is low, as well as cause damage when it's too low.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    private void OnChargeChanged(Entity<JellidComponent> entity, ref ChargeChangedEvent args)
    {
        if (!TryComp<BatteryComponent>(entity, out var battery))
            return;

        const float alertChange = 300f;
        if (battery.CurrentCharge <= alertChange)
        {
            _alerts.ShowAlert(entity.Owner, battery.NoBatteryAlert);
        }
        else
        {
            _alerts.ClearAlert(entity.Owner, battery.NoBatteryAlert);
        }

        const float damageCharge = 20f;
        if (battery.CurrentCharge >= damageCharge)
        {
            _previousCharges[entity] = args.Charge;
            return;
        }

        // Only damage if not the Jellid is NOT charging
        var isCharging = _previousCharges.TryGetValue(entity, out var prevCharge) && args.Charge > prevCharge;

        if (isCharging)
            return;

        var damage = new DamageSpecifier
        {
            DamageDict = { ["Slash"] = 0.1f }
        };
        _damageable.TryChangeDamage(entity.Owner, damage, origin: entity);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var playerQuery = EntityQueryEnumerator<JellidComponent, HandsComponent, BatteryComponent>();
        while (playerQuery.MoveNext(out var playerUid, out _, out _, out var internalBattery))
        {
            // Check for fireproof gloves before charging. They also block charging, as requested by Pix.
            if (_inventory.TryGetSlotEntity(playerUid, "gloves", out var glovesUid)
                && _tag.HasTag(glovesUid.Value, FireproofTag))
                continue;

            if (_hands.GetActiveItem(playerUid) is not { } heldItem)
                continue;

            if (!TryComp<BatteryComponent>(heldItem, out var containerBattery))
                continue;

            DrainPower(heldItem, containerBattery, playerUid, internalBattery, frameTime);
        }
    }

    private void DrainPower(EntityUid containerUid,
        BatteryComponent containerBattery,
        EntityUid internalUid,
        BatteryComponent internalBattery,
        float frameTime)
    {
        var drainAmount = Math.Min(containerBattery.CurrentCharge, 0.5f * frameTime);

        if (drainAmount <= 0)
            return;

        _battery.SetCharge(containerUid, containerBattery.CurrentCharge - drainAmount);
        _battery.SetCharge(internalUid, internalBattery.CurrentCharge + drainAmount);
    }
}
