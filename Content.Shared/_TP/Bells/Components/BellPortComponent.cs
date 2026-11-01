using Content.Shared._TP.Bells.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._TP.Bells.Components;

[RegisterComponent]
public sealed partial class BellPortComponent : Component
{
    [DataField(required: true)]
    public ProtoId<BellTypePrototype> BellType;

    /// <summary>
    ///     Display name for the UI list.
    /// </summary>
    [DataField]
    public string PortName = string.Empty;

    /// <summary>
    ///     If true, bells of this type are sent away and locked during a Flash Storm.
    /// </summary>
    [DataField]
    public bool StormShelter;
}