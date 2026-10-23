// Taken from https://github.com/emberfall-14/emberfall/pull/4/files with permission

using Content.Shared.Shuttles.Systems;
using Content.Shared.Timing;
using Robust.Shared.Serialization;

namespace Content.Shared._EmberFall.Bell;

[Serializable, NetSerializable]
public enum BellUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class BellConsoleState(FTLState ftlState, StartEndTime ftlTime, List<Components.BellDestination> destinations) : BoundUserInterfaceState
{
    public FTLState FTLState = ftlState;
    public StartEndTime FTLTime = ftlTime;
    public List<Components.BellDestination> Destinations = destinations;
}

[Serializable, NetSerializable]
public sealed class DockingConsoleFTLMessage(int index) : BoundUserInterfaceMessage
{
    public int Index = index;
}
