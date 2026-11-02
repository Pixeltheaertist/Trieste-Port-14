using Content.Shared._TP.Bells.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._TP.Bells.Components;

[RegisterComponent]
public sealed partial class BellComponent : Component
{
    [DataField(required: true)] public ProtoId<BellTypePrototype> BellType;

    [DataField] public bool Locked;
    [DataField] public bool Unsummoned;

    public EntityUid? CurrentPort;
    public TimeSpan? TransitStartTime;
    public TimeSpan? TransitEndTime;

    /// <summary>
    ///     Set during a Flash Storm until the bell has been sent to its storm shelter.
    ///     Kept around if the bell is mid-FTL or cooling down, so it retries once it can leave.
    /// </summary>
    public bool StormRecallPending;
}

public enum BellState : byte
{
    Docked,
}