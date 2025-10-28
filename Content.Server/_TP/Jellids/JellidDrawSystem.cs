using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._TP.Jellids;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
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

    private float _drainAmount = 0.5f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JellidComponent, ChargeChangedEvent>(OnChargeChanged);
    }

    private void OnChargeChanged(Entity<JellidComponent> entity, ref ChargeChangedEvent args)
    {
        if (!TryComp<BatteryComponent>(entity.Owner, out var battery))
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
        if (!(battery.CurrentCharge < damageCharge))
            return;

        if (Charging)
        {
        }
        else
        {
            var damage = new DamageSpecifier
            {
                DamageDict = { ["Slash"] = 0.1f }
            };
            _damageable.TryChangeDamage(entity.Owner, damage, origin: entity.Owner);
        }
    }

    private static readonly ProtoId<TagPrototype> FireproofTag = "PreventsFire";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var playerQuery = EntityQueryEnumerator<HandsComponent>();
        while (playerQuery.MoveNext(out var playerUid, out _))
        {
            if (!HasComp<JellidComponent>(playerUid))
                continue;

            if (_inventory.TryGetSlotEntity(playerUid, "gloves", out var glovesUid))
            {
                if (!HasComp<TagComponent>(glovesUid))
                {
                    continue;
                }

                if (_tag.HasTag(glovesUid.Value, FireproofTag))
                {
                    continue;
                }
            }

            if (_hands.GetActiveItem(playerUid) is not { } heldItem)
                continue;

            if (!TryComp<BatteryComponent>(heldItem, out var containerBattery))
                continue;

            if (!TryComp<BatteryComponent>(playerUid, out var internalBattery))
                continue;

            // Drain power from the held item's battery into the player's internal battery
            DrainPower(containerBattery, internalBattery);
        }
    }

    private void DrainPower(BatteryComponent containerBattery, BatteryComponent internalBattery)
    {
        _drainAmount = Math.Min(containerBattery.CurrentCharge, 0.5f);

        // If there's charge to drain
        if (!(_drainAmount > 0))
            return;

        // Directly use the BatterySystem to change the charge values
        _battery.SetCharge(containerBattery.Owner, containerBattery.CurrentCharge - _drainAmount, containerBattery);
        _battery.SetCharge(internalBattery.Owner, internalBattery.CurrentCharge + _drainAmount, internalBattery);
    }

    private bool Charging => _drainAmount > 0;
}
