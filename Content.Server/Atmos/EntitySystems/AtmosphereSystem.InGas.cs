using Content.Shared._TP.WaterInteractions;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;

namespace Content.Server.Atmos.EntitySystems;

/// <summary>
/// TRIESTE SPECIFIC
/// Handles detecting whether an entity is in a given gas and applying effects if so.
/// </summary>
public sealed partial class AtmosphereSystem
{
    private const float UpdateTimer = 1f;
    private float _timer;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public bool InGas(EntityUid uid, int? gasId = null, float? gasThreshold = null)
    {
        var mixture = _atmosphere.GetContainingMixture(uid);
        TryComp<_TP.Atmos.InGasComponent>(uid, out var inGas);
        //Use provided data if no component present
        if (inGas == null)
        {
            if (gasId == null || gasThreshold == null)
            {
                throw new Exception("Missing gasId and/or gasThreshold in InGas call");
            }

            return (mixture != null && mixture.GetMoles((int)gasId) >= gasThreshold);
        }

        //If we are not in the gas return false, else true
        return (mixture != null && mixture.GetMoles(inGas.GasId) >= inGas.GasThreshold);
    }

    private bool InWater(EntityUid uid, int? gasId = 9)
    {
        // Check for an 'InGasComponent' on the entity.
        if (!TryComp<_TP.Atmos.InGasComponent>(uid, out var inGas))
        {
            return false;
        }

        // Now we set some variables for Transform and grid uid.
        // If the uid IS null, we return false.
        var xForm = Transform(uid);
        var gridUid = xForm.GridUid;
        if (gridUid == null)
        {
            return false;
        }

        // Now we check for a 'GridAtmosphereComponent' on the grid.
        // If the grid uid IS null, we return false.
        if (!TryComp<GridAtmosphereComponent>(gridUid.Value, out var gridAtmos))
        {
            return false;
        }

        // Now we use the transform system to get the tile position of water.
        // If the tile has no atmosphere, or the value is null, we return false and set the water amount to 0.
        var tile = _transform.GetGridOrMapTilePosition(uid, xForm);
        if (!gridAtmos.Tiles.TryGetValue(tile, out var tileAtmos) || tileAtmos.Air == null)
        {
            return false;
        }

        // Finally, we check if the water amount is greater than 2.
        // If so, we return true and set the water to the moles of water gasses.
        var waterMoles = tileAtmos.Air.GetMoles(Gas.Water);
        if (waterMoles > 2f)
        {
            inGas.WaterAmount = waterMoles;
            return true;
        }

        inGas.WaterAmount = 0f;
        return false;
    }

    private Dictionary<EntityUid, TimeSpan> _timeInWater = new();
    private Dictionary<EntityUid, TimeSpan> _timeLeftWater = new();

    public void UpdateInGas(float frameTime)
    {
        _timer += frameTime;

        if (_timer < UpdateTimer)
            return;

        _timer -= UpdateTimer;

        var currTime = _gameTiming.CurTime;

        // Two lists of entities we want to process
        // One is a list of entities that we should process for damage,
        // the other is a list of entities that are in water.
        var entitiesToProcess = new List<(EntityUid uid, _TP.Atmos.InGasComponent inGas, DamageableComponent damageable)>();
        var entitiesInWater = new List<(EntityUid, _TP.Atmos.InGasComponent inGas)>();
        var puddlesToDelete = new List<EntityUid>();

        var waterEnumerator = EntityQueryEnumerator<_TP.Atmos.InGasComponent>();
        while (waterEnumerator.MoveNext(out var uid, out var inGas))
        {
            entitiesInWater.Add((uid, inGas));
        }

        var damageEnumerator = EntityQueryEnumerator<_TP.Atmos.InGasComponent, DamageableComponent>();
        while (damageEnumerator.MoveNext(out var uid, out var inGas, out var damageable))
        {
            if (!inGas.DamagedByGas)
            {
                continue;
            }

            entitiesToProcess.Add((uid, inGas, damageable));
        }

        foreach (var (uid, inGas) in entitiesInWater)
        {
            var currentlyInWater = InWater(uid);
            inGas.InWater = currentlyInWater;

            // Puddle deletion system
            if (TryComp<PuddleComponent>(uid, out _) && currentlyInWater)
            {
                puddlesToDelete.Add(uid);
                continue;
            }

            if (currentlyInWater)
            {
                _timeLeftWater.Remove(uid);

                _timeInWater.TryGetValue(uid, out var timeInWater);
                _timeInWater[uid] = timeInWater + TimeSpan.FromSeconds(UpdateTimer);

                if (_timeInWater[uid] > TimeSpan.FromSeconds(3))
                {
                    RaiseNetworkEvent(new InGasEvent(GetNetEntity(uid), true), uid);
                }
            }
            else if (_timeInWater.ContainsKey(uid))
            {
                _timeLeftWater.TryAdd(uid, currTime);

                if (currTime - _timeLeftWater[uid] > TimeSpan.FromSeconds(5))
                {
                    RaiseNetworkEvent(new InGasEvent(GetNetEntity(uid), false), uid);
                    _timeInWater.Remove(uid);
                    _timeLeftWater.Remove(uid);
                }
            }
        }

        // Now process the entities
        foreach (var (uid, inGas, damageable) in entitiesToProcess)
        {
            var currentlyInWater = InWater(uid);
            inGas.InWater = currentlyInWater;

            if (!currentlyInWater)
            {
                if (inGas.TakingDamage)
                {
                    inGas.TakingDamage = false;
                    _alerts.ClearAlertCategory(uid, inGas.BreathingAlertCategory);
                    _adminLog.Add(LogType.Electrocution, $"Entity {uid} is no longer taking damage from water.");
                }

                continue;
            }

            var totalDamage = FixedPoint2.Zero;
            foreach (var (damageType, _) in inGas.Damage.DamageDict)
            {
                if (!damageable.Damage.DamageDict.TryGetValue(damageType, out var damage))
                    continue;
                totalDamage += damage;
            }

            if (totalDamage >= inGas.MaxDamage)
            {
                continue;
            }

            _damage.TryChangeDamage(uid, inGas.Damage, true);
            if (!inGas.TakingDamage)
            {
                inGas.TakingDamage = true;
                _adminLog.Add(LogType.Electrocution, $"Entity {uid} is now taking damage from water.");
                _alerts.ShowAlert(uid, inGas.DamageAlert, 1);
            }
        }

        foreach (var puddle in puddlesToDelete)
        {
            QueueDel(puddle);
        }
    }
}
