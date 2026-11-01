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
}

public enum BellState : byte
{
    Docked,
}