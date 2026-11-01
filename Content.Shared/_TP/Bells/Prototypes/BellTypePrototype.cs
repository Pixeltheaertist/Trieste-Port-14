using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._TP.Bells.Prototypes;

[Prototype]
public sealed partial class BellTypePrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField] public LocId Name { get; private set; }

    #region Tuning

    [DataField] public float? StartupTime;
    [DataField] public float? HyperspaceTime;
    [DataField] public string? PriorityTag;

    [DataField] public float? CooldownTime;

    #endregion


    [DataField(required: true)] public ResPath ShuttlePath;
}