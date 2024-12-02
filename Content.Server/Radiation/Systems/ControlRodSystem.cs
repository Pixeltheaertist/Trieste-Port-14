using JetBrains.Annotations;
using Content.Server.DeviceLinking.Components;
using Content.Server.Power.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Server.DeviceLinking.Events;
using Content.Server.UserInterface;
using Content.Server.Power.EntitySystems;
using Robust.Server.GameObjects;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceLinking;
using Content.Server.Radiation.Components;
using Content.Shared.Radiation.Components;
namespace Content.Server.Radiation.Systems
{
    [UsedImplicitly]
    public sealed class ControlRodSystem : EntitySystem
    {
        [Dependency] private readonly SharedDeviceLinkSystem _linkSystem = default!;
        [Dependency] private readonly ControlRodConsoleSystem _controlRodConsoleSystem = default!;
        [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly RadiationSystem _radiation = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ControlRodComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<ControlRodComponent, PortDisconnectedEvent>(OnPortDisconnected);
            SubscribeLocalEvent<ControlRodComponent, AnchorStateChangedEvent>(OnAnchor);
        }
        private enum command : byte
        {
            Extend,
            Retract
        }
        private Dictionary<EntityUid, Queue<command>> _commandQueueDict = new Dictionary<EntityUid, Queue<command>>();
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            //for each rod in dict, check power, check the timer - pop the queue and run the appropriate function if time is up - update the timer
            foreach (KeyValuePair<EntityUid,Queue<command>> entry in _commandQueueDict)
            {
                if (!_powerReceiverSystem.IsPowered(entry.Key))
                    continue;
                if (TryComp<ControlRodComponent>(entry.Key, out var controlRod))
                {
                    if (entry.Value.Count == 0)
                    {
                        controlRod.Timer = 0f;
                        continue;
                    }
                    controlRod.Timer += frameTime;
                    if (controlRod.Timer >= controlRod.CommandTime)
                    {
                        var job = entry.Value.Dequeue();
                        switch (job) {
                            case command.Extend:
                                ExtendRod(entry.Key);
                                break;
                            case command.Retract:
                                RetractRod(entry.Key);
                                break;
                        }
                        controlRod.Timer = 0f;
                    }
                }
            }
        }
        private void OnComponentInit(EntityUid uid, ControlRodComponent component, ComponentInit args)
        {
            _linkSystem.EnsureSourcePorts(uid, ControlRodComponent.ControlRodPort);
        }
        private void OnPortDisconnected(EntityUid uid, ControlRodComponent component, PortDisconnectedEvent args)
        {
            if (component.ConnectedConsole != null && TryComp<ControlRodConsoleComponent>(component.ConnectedConsole, out var console))
            {
                //remove from lists (and in range dict)
                for (var i = 0; i < console.ControlRods.Count; i++)
                {
                    if (console.ControlRods[i] == uid)
                    {
                        console.ControlRods.RemoveAt(i);
                        console.RodsInRange.Remove(i);
                        break;
                    }
                }
                _controlRodConsoleSystem.UpdateUserInterface(console);
            }

            component.ConnectedConsole = null;
        }
        private void OnAnchor(EntityUid uid, ControlRodComponent component, ref AnchorStateChangedEvent args)
        {
            if (component.ConnectedConsole == null || !TryComp<ControlRodConsoleComponent>(component.ConnectedConsole, out var console))
                return;
            if (args.Anchored)
            {
                _controlRodConsoleSystem.RecheckConnections(component.ConnectedConsole.Value, console.ControlRods, console);
                return;
            }
            _controlRodConsoleSystem.UpdateUserInterface(console);
        }
        public void ExtendAllRodsCommand(List<EntityUid> rods)
        {
            //check power
            foreach (var rod in rods)
            {
                if (!_powerReceiverSystem.IsPowered(rod))
                    return;
                if (TryComp<ControlRodComponent>(rod, out var controlRod))
                {
                    if (!_commandQueueDict.ContainsKey(rod))
                    {
                        _commandQueueDict[rod] = new Queue<command>();
                    }
                    var numSteps = (int) Math.Ceiling((controlRod.MaxExtension - controlRod.CurrentExtension) / controlRod.ExtensionStep);
                    for (var i = 0; i < numSteps; i++)
                        _commandQueueDict[rod].Enqueue(command.Extend);
                }
            }
        }
        public void ExtendRodCommand(EntityUid? uid)
        {

            if (!(uid is null))
            {
                //check power
                if (!_powerReceiverSystem.IsPowered(uid.Value))
                    return;
                //add to queue
                if (TryComp<ControlRodComponent>(uid, out var controlRod))
                {
                    if (!_commandQueueDict.ContainsKey(uid.Value))
                    {
                        _commandQueueDict[uid.Value] = new Queue<command>();
                    }
                    _commandQueueDict[uid.Value].Enqueue(command.Extend);
                }
            }
        }
        public void RetractRodCommand(EntityUid? uid)
        {

            if (!(uid is null))
            {
                //check power
                if (!_powerReceiverSystem.IsPowered(uid.Value))
                    return;
                //add to queue
                if (TryComp<ControlRodComponent>(uid, out var controlRod))
                {
                    if (!_commandQueueDict.ContainsKey(uid.Value))
                    {
                        _commandQueueDict[uid.Value] = new Queue<command>();
                    }
                    _commandQueueDict[uid.Value].Enqueue(command.Retract);
                }
            }
        }
        public void StopRodCommand(EntityUid? uid)
        {
            //cancel queue
            if (uid is null)
            {
                _commandQueueDict.Clear();
            }
            else
            {
                //check power
                if (!_powerReceiverSystem.IsPowered(uid.Value))
                    return;
                _commandQueueDict[uid.Value].Clear();
            }
        }

        /**
         * Simulate shutdown vs control rods by operating control rods below
         * 100% extension (shutdown) in a smaller linear range.
         */
        private float ControlLaw(ControlRodComponent rod)
        {
            float setting = rod.CurrentExtension;
            if (setting > 0.99f)
            {
                return rod.ShutdownRange + rod.ControlRange;
            }
            else {
                return setting * rod.ControlRange;
            }
        }

        private void UpdateRodValues(EntityUid uid, ControlRodComponent controlRod)
        {
            //set rad blocker value
            if (TryComp<RadiationBlockerComponent>(uid, out var radBlocker))
            {
                _radiation.SetBlocking(uid, radBlocker, ControlLaw(controlRod));
            }
            //determine stage
            var stage = (int)(controlRod.CurrentExtension * (controlRod.MaxExtension / controlRod.ExtensionStep));
            //update sprite
            _appearance.SetData(uid, ControlRodVisuals.Status, (ControlRodStatus)stage);
            if (controlRod.ConnectedConsole != null)
                if (TryComp<ControlRodConsoleComponent>(controlRod.ConnectedConsole.Value, out var console))
                    _controlRodConsoleSystem.UpdateUserInterface(console);

        }
        private void ExtendRod(EntityUid uid)
        {
            if (TryComp<ControlRodComponent>(uid, out var controlRod))
            {
                //add step to rod extension if able
                if (controlRod.CurrentExtension < controlRod.MaxExtension)
                {
                    controlRod.CurrentExtension += controlRod.ExtensionStep;
                    if (controlRod.CurrentExtension > controlRod.MaxExtension)
                        controlRod.CurrentExtension = controlRod.MaxExtension;
                    UpdateRodValues(uid, controlRod);
                }
            }
        }
        private void RetractRod(EntityUid uid)
        {
            if (TryComp<ControlRodComponent>(uid, out var controlRod))
            {
                //remove step to rod extension if able
                if (controlRod.CurrentExtension > 0)
                {
                    controlRod.CurrentExtension -= controlRod.ExtensionStep;
                    if (controlRod.CurrentExtension < 0)
                        controlRod.CurrentExtension = 0;
                    UpdateRodValues(uid, controlRod);
                }
            }
        }
    }
}
