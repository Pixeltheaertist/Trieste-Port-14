using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Content.Shared.Random;

namespace Content.Shared._TP.Aberrant;

/// <summary>
/// A collection of effects that can be randomly applied to an entity when it reaches a certain level of aberrant damage.
/// Not sure if we actually need this tbh, i might just need to learn entityTables
/// </summary>
[Prototype("aberrantTable")]
[DataDefinition]
public sealed partial class AberrantTablePrototype : IWeightedRandomPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set;} = default!;

    [DataField(customTypeSerializer: typeof(PrototypeIdDictionarySerializer<float, AberrantEffectPrototype>))]

    public Dictionary<string, float> Weights { get; private set; } = new();

    //[DataField("effects")]
    //public Dictionary<AberrantEffect, float> Effects= new();
}
