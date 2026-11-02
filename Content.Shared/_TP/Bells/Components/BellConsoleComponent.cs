using Content.Shared._TP.Bells.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._TP.Bells.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BellConsoleComponent : Component
{
    [DataField(required: true)]
    public ProtoId<BellTypePrototype> BellType;
}