using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._TP.Aberrant;

/// <summary>
/// A collection of effects that can be randomly applied to an entity when it reaches a certain level of aberrant damage.
/// </summary>
[Prototype("aberrantTable")]
[DataDefinition]
public sealed partial class AberrantTablePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set;} = default!;

    [DataField("effects")]
    public Dictionary<AberrantEffect, float> Effects= new();
}
