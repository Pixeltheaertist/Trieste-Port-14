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

    ///<summary>
    ///    Component for an aberrant effect
    ///</summary>
    [DataField]
    public List<Component> Components = new();
}
