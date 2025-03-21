using Robust.Shared.Prototypes;

namespace Content.Shared._TP.Aberrant;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype("aberrantEffectCommpound")]
[DataDefinition]
public sealed partial class AberrantEffectCompoundPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set;} = default!;

    /// <summary>
    ///     Stores the effects making up this effect. List incase we want compound effects
    /// </summary>
    [DataField("effects")]
    public List<AberrantEffect> Effects = new();
}

[Prototype("aberrantEffect")]
[DataDefinition]
public sealed partial class AberrantEffectPrototype : IPrototype
{
    [IdDatafield]
    public string ID { get; private set;} = default!;

    ///<summary>
    ///    Component for an aberrant effect
    ///</summary>
    [DataField]
    public List<Component> Components = new();
}
