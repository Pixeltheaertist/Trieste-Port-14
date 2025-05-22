using Content.Server.Administration.Logs;
using Content.Server.Atmos.Components;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using JetBrains.Annotations;

namespace Content.Server.Atmos.EntitySystems;

/// <summary>
/// Handles detecting wether an entity is in a given gas, and applying effects if so.
/// </summary>
public sealed class InGasSystem : EntitySystem
{
    private const float UpdateTimer = 1f;
    private float _timer = 0f;
    [Dependency] private readonly AtmosphereSystem _atmo = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    public bool InGas(EntityUid uid, int? gasId = null, float? gasThreshold = null)
        {
            var mixture = _atmo.GetContainingMixture(uid);
            var inGas = EntityManager.GetComponent<InGasComponent>(uid);
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

     public bool InWater(EntityUid uid, int? gasId = 9)
        {
            var mixture = _atmo.GetContainingMixture(uid);
            var inGas = EntityManager.GetComponent<InGasComponent>(uid);

            if (mixture == null)
            {
                return false;
            }

            if (!gasId.HasValue)
            {
                return false;
            }

            if (mixture.GetMoles(Gas.Water) > 2f)
            {
                inGas.WaterAmount = mixture.GetMoles(Gas.Water); // Gets the amount of water around you
                return true;
            }

            inGas.WaterAmount = 0f;
            return false;
        }

   public override void Update(float frameTime)
        {
            _timer += frameTime;

            if (_timer < UpdateTimer)
                return;

            _timer -= UpdateTimer;

            // A list of entities we want to process
            // We were getting an InvalidOperationException, so this -should- fix it. -Cookie
            var entitiesToProcess = new List<(EntityUid uid, InGasComponent inGas, DamageableComponent damageable)>();

            var enumerator = EntityQueryEnumerator<InGasComponent, DamageableComponent>();
            while (enumerator.MoveNext(out var uid, out var inGas, out var damageable))
            {
                if (!inGas.DamagedByGas)
                {
                    continue;
                }

                entitiesToProcess.Add((uid, inGas, damageable));
            }

            // Now process the entities
            foreach (var (uid, inGas, damageable) in entitiesToProcess)
            {

            var currentlyInWater = InWater(uid);
                inGas.InWater = currentlyInWater;

                // Update the water state in the component
                // Raise the event depending on whether it's entering or exiting water
                if (currentlyInWater)
                {
                    RaiseLocalEvent(new InWaterEvent(uid));
                }
                else
                {
                    RaiseLocalEvent(new OutOfWaterEvent(uid));
                }

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

                _damageable.TryChangeDamage(uid, inGas.Damage, true);
                if (!inGas.TakingDamage)
                {
                    inGas.TakingDamage = true;
                    _adminLog.Add(LogType.Electrocution, $"Entity {uid} is now taking damage from water.");
                    _alerts.ShowAlert(uid, inGas.DamageAlert, 1);
                }
            }
        }
}

