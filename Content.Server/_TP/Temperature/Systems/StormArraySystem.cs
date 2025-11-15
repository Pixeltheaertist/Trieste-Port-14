using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Chat.Systems;
using Content.Server.Destructible;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Temperature.Components;
using Content.Shared.Explosion.Components;
using Content.Shared.NodeContainer;
using Content.Shared.Temperature.Components;
using Content.Shared.Examine;
using Robust.Shared.Audio;

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
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;

    private EntityQuery<NodeContainerComponent> _nodeContainerQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Components.StormArrayComponent, AtmosDeviceUpdateEvent>(OnAtmosUpdate);
        SubscribeLocalEvent<Components.StormArrayComponent, ExaminedEvent>(OnExamine);

        _nodeContainerQuery = GetEntityQuery<NodeContainerComponent>();
    }

    private void OnExamine(Entity<Components.StormArrayComponent> ent, ref ExaminedEvent args)
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
        var selfHeating = Math.Abs(radiantTempComp.EnergyChangedPerSecond) * 0.035f * args.dt;
        tempComp.CurrentTemperature += selfHeating;

        // Now update the coolant AFTER the heating, in a separate function.
        UpdateCoolant(ent, ref args);

        // Then we announce if the temperature is too high, based on the thresholds.
        // This is also a separate function UNLESS the temperature is 500,
        // in which case it will explode.
        if (tempComp.CurrentTemperature >= 100)
        {
            Announcement(Loc.GetString("storm-array-alert-1"),
                tempComp.CurrentTemperature >= 150,
                ref arrayComp.FirstAnnouncement);

            Announcement(Loc.GetString("storm-array-alert-2"),
                tempComp.CurrentTemperature >= 300,
                ref arrayComp.SecondAnnouncement);

            Announcement(Loc.GetString("storm-array-alert-3"),
                tempComp.CurrentTemperature >= 450,
                ref arrayComp.ThirdAnnouncement);

            // This part handles the explosion at 500 degrees.
            // If the explosion component doesn't exist, however, we return. (This shouldn't happen!)
            if (!TryComp<ExplosiveComponent>(ent, out var explComp))
                return;

            if (tempComp.CurrentTemperature >= 500)
                _destructible.ExplosionSystem.TriggerExplosive(ent.Owner, explComp, true, explComp.TotalIntensity);
        }
    }

    private void Announcement(string msg, bool when, ref bool announcementFlag)
    {
        if (!when || announcementFlag)
            return;

        var sound = new SoundPathSpecifier("/Audio/Announcements/bloblarm.ogg");

        _chat.DispatchGlobalAnnouncement(msg,
            Loc.GetString("admin-announce-announcer-default"),
            true,
            sound,
            Color.Red);

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

        // SIMPLIFIED FLOW SYSTEM: Just transfer a fixed percentage of inlet gas per second
        // This acts like a built-in pump - no pressure differential needed!
        var transferRate = 0.5f; // Transfer 50% of inlet gas per second
        var transferMoles = inlet.Air.TotalMoles * transferRate * args.dt;

        // Check if we have any gas to work with
        if (inlet.Air.TotalMoles <= 0 || transferMoles <= 0)
        {
            comp.LastCoolingRate = 0;
            comp.LastCoolantFlow = 0;
            comp.StatusMessage = "NO COOLANT: Inlet has no gas";
            return;
        }

        // Remove gas from inlet
        var coolantGas = inlet.Air.Remove(transferMoles);

        var coolantHeatCapacity = _atmosphere.GetHeatCapacity(coolantGas, true);

        if (coolantHeatCapacity <= 0)
        {
            comp.StatusMessage = "COOLANT ERROR: Zero heat capacity";
            _atmosphere.Merge(outlet.Air, coolantGas);
            return;
        }

        // Calculate temperature difference
        var tempDifference = temp.CurrentTemperature - coolantGas.Temperature;

        if (tempDifference <= 0)
        {
            comp.StatusMessage =
                $"COOLANT WARMER: Coolant ({coolantGas.Temperature:F1}K) is warmer than array ({temp.CurrentTemperature:F1}K)";
            _atmosphere.Merge(outlet.Air, coolantGas);
            return;
        }

        // Use a proper heat capacity instead of HeatDamageThreshold
        // This represents the thermal mass of the Storm Array structure
        const float entityHeatCapacity = 50000f; // ~100kg of steel equivalent (Math by AI)

        // Maximum heat transfer based on coolant capacity
        var maxHeatFromTempDiff = tempDifference * coolantHeatCapacity;

        // Heat transfer is limited only by the coolant's capacity
        var heatTransferred = maxHeatFromTempDiff;

        // Apply the efficiency factor
        heatTransferred *= comp.CoolingEfficiency;

        // Cool the entity
        var entityTempChange = heatTransferred / entityHeatCapacity;
        temp.CurrentTemperature -= entityTempChange;

        // Heat the coolant gas
        var coolantTempChange = heatTransferred / coolantHeatCapacity;
        coolantGas.Temperature += coolantTempChange;

        // Store stats for monitoring/visuals
        comp.LastCoolingRate = heatTransferred / args.dt;
        comp.LastCoolantFlow = transferMoles;
        comp.StatusMessage = $"COOLING: {comp.LastCoolingRate / 1000:F1} kW | Flow: {comp.LastCoolantFlow:F2} mol/s";

        // Merge heated coolant back into the outlet
        _atmosphere.Merge(outlet.Air, coolantGas);
    }
}
