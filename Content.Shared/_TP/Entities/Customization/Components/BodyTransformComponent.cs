using Content.Shared.DisplacementMap;

namespace Content.Shared._TP.Entities.Customization.Components;

[RegisterComponent]
public sealed partial class BodyTransformComponent : Component
{
    [DataField]
    public string NewSpecies;

    [DataField]
    public Dictionary<string, DisplacementData> Displacements { get; set; }
}
