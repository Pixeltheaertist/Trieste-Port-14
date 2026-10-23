using System.ComponentModel.DataAnnotations;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._TP.Oil;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(OilProductionSystem))]
public sealed partial class OilProductionComponent : Component
{

    [DataField("solution", required: true)]
    public string SolutionName = string.Empty;

    [ViewVariables]
    public Entity<SolutionComponent>? SolutionRef = null;

    [DataField(required: true)]
    public Solution Generated = default!;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(1);

    [DataField("nextChargeTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextRegenTime = TimeSpan.FromSeconds(0);

    [DataField]
    public bool ValidContainer;
}
