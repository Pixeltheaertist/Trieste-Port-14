using Content.Shared.Shuttles.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared._TP.Bells.Misc;

[Serializable, NetSerializable]
public enum BellConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class BellConsoleBoundUserInterfaceState(
    List<BellDestinationInfo> destinations,
    bool locked,
    FTLState? ftlState,
    string? currentLocationName,
    TimeSpan? transitStart,
    TimeSpan? transitEnd)
    : BoundUserInterfaceState
{
    public List<BellDestinationInfo> Destinations = destinations;
    public bool Locked = locked;
    public FTLState? FtlState = ftlState;
    public string? CurrentLocationName = currentLocationName;
    public TimeSpan? TransitStart = transitStart;
    public TimeSpan? TransitEnd = transitEnd;
}

[Serializable, NetSerializable]
public sealed class BellDestinationInfo(NetEntity door, string name)
{
    public NetEntity Door = door;
    public string Name = name;
}

[Serializable, NetSerializable]
public sealed class BellConsoleTravelMessage(NetEntity destination) : BoundUserInterfaceMessage
{
    public NetEntity Destination = destination;
}
