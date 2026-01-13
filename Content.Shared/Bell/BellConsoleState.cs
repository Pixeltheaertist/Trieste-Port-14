// Taken from https://github.com/emberfall-14/emberfall/pull/4/files with permission

using Content.Shared.TP14.Bell.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Timing;
using Robust.Shared.Serialization;

namespace Content.Shared.TP14.Bell;

[Serializable, NetSerializable]
public enum BellUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class BellConsoleState(FTLState ftlState, StartEndTime ftlTime, List<BellDestination> destinations) : BoundUserInterfaceState
{
    public FTLState FTLState = ftlState;
    public StartEndTime FTLTime = ftlTime;
    public List<BellDestination> Destinations = destinations;
}

[Serializable, NetSerializable]
public sealed class DockingConsoleFTLMessage(int index) : BoundUserInterfaceMessage
{
    public int Index = index;
}
