using Content.Shared.DisplacementMap;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._TP.Entities.Customization.Components;

[RegisterComponent]
public sealed partial class BodyTransformComponent : Component
{
    [DataField(required: true)]
    public ProtoId<SpeciesPrototype> TargetSpecies;

    [DataField(required: true)]
    public string NewSpecies;

    [DataField]
    public Dictionary<string, DisplacementData>? Displacements;

    [DataField]
    public Dictionary<HumanoidVisualLayers, DisplacementData>? MarkingsDisplacement;
}
