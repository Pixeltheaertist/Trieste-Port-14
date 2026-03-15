using Robust.Shared.Prototypes;

namespace Content.Shared._TP.Entities.Customization.Components;

[RegisterComponent]
public sealed partial class OrganReplacementComponent : Component
{
    [DataField]
    public Dictionary<EntProtoId, EntProtoId> Replacements = new();
}
