using Robust.Shared.Prototypes;

namespace Content.Shared._TP.Aberrant;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype("aberrantEffect")]
[DataDefinition]
public sealed partial class AberrantEffectPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set;} = default!;

    /// <summary>
    ///     Stores the effects making up this effect. List incase we want compound effects
    /// </summary>
    [DataField("effects")]
    public List<AberrantEffect> Effects= new();

}

[Serializable]
[DataDefinition]
public partial struct AberrantEffect
{
    [DataField("id")]
    public EntProtoId? PrototypeId = null;

    [DataField]
    public List<Component> Components = new();
}
