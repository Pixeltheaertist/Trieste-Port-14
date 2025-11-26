using Content.Server._TP.Temperature.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Destructible;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Radio.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared.Atmos;
using Content.Shared.Examine;
using Content.Shared.Explosion.Components;
using Content.Shared.NodeContainer;
using Content.Shared.Temperature.Components;

namespace Content.Server._TP.Temperature.Systems;

/// <summary>
///     Systems handling the Storm Array.
///     This is similar to the TEG coolant loop, absorbing heat and transferring it to pipe gas.
///     Created by Cookie (Father Cheese) for Trieste Port 14.
/// </summary>
public sealed class StormArraySystem : EntitySystem
{
    // Pipe names from the Storm Array entity.
    private const string NodeNameInlet = "inlet";
    private const string NodeNameOutlet = "outlet";

    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly RadioSystem _radio = default!;

    private EntityQuery<NodeContainerComponent> _nodeContainerQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Components.StormArrayComponent, AtmosDeviceUpdateEvent>(OnAtmosUpdate);
        SubscribeLocalEvent<StormArrayComponent, ExaminedEvent>(OnExamined);

        _nodeContainerQuery = GetEntityQuery<NodeContainerComponent>();
    }

    private void OnExamined(Entity<StormArrayComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp<TemperatureComponent>(ent, out var temp))
            return;

        var comp = ent.Comp;

        // Show the internal temperature in both Kelvin and Celsius
        var tempC = temp.CurrentTemperature - 273.15f;
        args.PushMarkup(Loc.GetString("storm-array-examine-temperature",
            ("tempK", temp.CurrentTemperature.ToString("F1")),
            ("tempC", tempC.ToString("F1"))));

        // Show a status message if available
        if (!string.IsNullOrEmpty(comp.StatusMessage))
        {
            args.PushMarkup(Loc.GetString("storm-array-examine-status",
                ("status", comp.StatusMessage)));
        }

        // Display the cooling stats
        if (comp.LastCoolingRate > 0)
        {
            args.PushMarkup(Loc.GetString("storm-array-examine-cooling",
                ("rate", (comp.LastCoolingRate / 1000).ToString("F1"))));
        }
    }

    private void OnAtmosUpdate(Entity<Components.StormArrayComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        // Get the nodes from the entity for Radiant Temperature and Temperature.
        // We also make a variable for the storm array component, for easy access.
        if (!TryComp<RadiantTemperatureComponent>(ent, out var radiantTempComp))
            return;

        if (!TryComp<TemperatureComponent>(ent, out var tempComp))
            return;

        var arrayComp = ent.Comp;

        // Now we heat the entity based on how much entity it's making.
        // This is always set to 100KW. (for now)
        var selfHeating = Math.Abs(radiantTempComp.EnergyChangedPerSecond) * 0.33f * args.dt;
        tempComp.CurrentTemperature += selfHeating;

        // Now update the coolant AFTER the heating, in a separate function.
        UpdateCoolant(ent, ref args);

        // Then we announce if the temperature is too high, based on the thresholds.
        // This is also a separate function UNLESS the temperature is 500,
        // in which case it will explode.
        Announcement(ent,
            Loc.GetString("storm-array-alert-1"),
            tempComp.CurrentTemperature >= 411,
            ref arrayComp.FirstAnnouncement);

        Announcement(ent,
            Loc.GetString("storm-array-alert-2"),
            tempComp.CurrentTemperature >= 822,
            ref arrayComp.SecondAnnouncement);

        Announcement(ent,
            Loc.GetString("storm-array-alert-3"),
            tempComp.CurrentTemperature >= 1233,
            ref arrayComp.ThirdAnnouncement);

        // This part handles the explosion at 500 degrees.
        // If the explosion component doesn't exist, however, we return. (This shouldn't happen!)
        if (!TryComp<ExplosiveComponent>(ent, out var explComp))
            return;

        if (tempComp.CurrentTemperature >= 1644)
            _destructible.ExplosionSystem.TriggerExplosive(ent.Owner, explComp, true, explComp.TotalIntensity);
    }

    private void Announcement(Entity<StormArrayComponent> ent, string msg, bool when, ref bool announcementFlag)
    {
        if (!when || announcementFlag)
            return;

        _radio.SendRadioMessage(ent.Owner, msg, "Engineering", ent.Owner);

        announcementFlag = true;
    }

    private void UpdateCoolant(Entity<Components.StormArrayComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        // Set a StormArrayComponent variable, for easy access.
        // We also get the nodes from the entity for Coolant and Temperature.
        var comp = ent.Comp;
        if (!TryComp<TemperatureComponent>(ent, out var temp))
            return;

        if (!_nodeContainerQuery.TryGetComponent(ent, out var nodeContainer))
            return;

        if (!nodeContainer.Nodes.TryGetValue(NodeNameInlet, out var inletNode) ||
            !nodeContainer.Nodes.TryGetValue(NodeNameOutlet, out var outletNode))
            return;

        // Assign the nodes to inlet and outlet variables.
        var inlet = (PipeNode)inletNode;
        var outlet = (PipeNode)outletNode;

        if (inlet.Air.TotalMoles <= 0)
        {
            comp.LastCoolingRate = 0;
            comp.LastCoolantFlow = 0;
            comp.StatusMessage = "NO COOLANT: Inlet has no gas";
            return;
        }

        // Calculate gas transfer based on pressure difference, then
        // calculate heat difference from the Array and coolant.
        // We return if there's no coolant or no heat capacity.
        var (coolantGas, pressureDelta) = GetCoolantTransfer(inlet.Air, outlet.Air);

        if (coolantGas.TotalMoles <= 0)
            return;

        var coolantHeatCapacity = _atmosphere.GetHeatCapacity(coolantGas, true);

        if (coolantHeatCapacity <= 0)
        {
            comp.StatusMessage = "COOLANT ERROR: Zero heat capacity";
            _atmosphere.Merge(outlet.Air, coolantGas);
            return;
        }

        // Calculate maximum heat that can be transferred
        // Limited by either the temperature difference or the entity's cooling rate
        var tempDifference = temp.CurrentTemperature - coolantGas.Temperature;

        if (tempDifference <= 0)
        {
            comp.StatusMessage =
                $"COOLANT WARMER: Coolant ({coolantGas.Temperature:F1}K) is warmer than array ({temp.CurrentTemperature:F1}K)";
            _atmosphere.Merge(outlet.Air, coolantGas);
            return;
        }

        // Maximum heat transfer based on coolant capacity
        var maxHeatFromTempDiff = tempDifference * coolantHeatCapacity;

        // Maximum heat transfer based on cooling rate (joules per second)
        var maxHeatFromRate = 50000 * args.dt;

        // Take the minimum of the two limits
        var heatTransferred = MathF.Min(maxHeatFromTempDiff, maxHeatFromRate);

        // Apply the efficiency factor
        heatTransferred *= comp.CoolingEfficiency;

        // Cool the entity
        var entityHeatCapacity = temp.HeatDamageThreshold;
        var entityTempChange = heatTransferred / entityHeatCapacity;
        temp.CurrentTemperature -= entityTempChange;

        // Heat the coolant gas
        var coolantTempChange = heatTransferred / coolantHeatCapacity;
        coolantGas.Temperature += coolantTempChange;

        // Store stats for monitoring/visuals
        comp.LastCoolingRate = heatTransferred / args.dt;
        comp.LastCoolantFlow = coolantGas.TotalMoles;

        comp.StatusMessage = $"COOLING: {comp.LastCoolingRate / 1000:F1} kW | Flow: {comp.LastCoolantFlow:F2} mol/s";

        comp.LastPressureDelta = pressureDelta;

        // Merge heated coolant back into the outlet
        _atmosphere.Merge(outlet.Air, coolantGas);
    }

    private static (GasMixture gas, float pressureDelta) GetCoolantTransfer(GasMixture airInlet, GasMixture airOutlet)
    {
        var mole1 = airInlet.TotalMoles;
        var mole2 = airOutlet.TotalMoles;
        var pr1 = airInlet.Pressure;
        var pr2 = airOutlet.Pressure;
        var vol1 = airInlet.Volume;
        var vol2 = airOutlet.Volume;
        var temp1 = airInlet.Temperature;
        var temp2 = airOutlet.Temperature;

        var presDiff = pr1 - pr2;

        var deNom = temp1 * vol2 + temp2 * vol1;

        var transferMoles = mole1 - (mole1 + mole2) * temp2 * vol1 / deNom;
        return (airInlet.Remove(transferMoles), presDiff);

    }
}
