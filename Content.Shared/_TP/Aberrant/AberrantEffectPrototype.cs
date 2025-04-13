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
    ///     Components making up the effect go here. Also works for compound effects if we wanna be fancy
    ///     This should be things like shuddering, or other brief effects
    /// </summary>
    [DataField("temporaryComponents", serverOnly: true)]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Components that are permanently added to an entity when the effect's node is entered.
    /// This will be things like that 'madman' antag that was mentioned in discord
    /// Yes I am stealing a lot of arti code
    /// </summary>
    [DataField("permanentComponents")]
    public ComponentRegistry PermanentComponents = new();
}
