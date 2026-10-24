using Robust.Shared.Prototypes;

namespace Content.Shared._TP.Kitchen;

/// <summary>
///     A recipe for deep fryers.
/// </summary>
[Prototype]
public sealed partial class DeepFryingRecipePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name")]
    private string _name = string.Empty;

    [DataField]
    public string Group = "Other";

    [DataField("time")]
    public uint CookTime { get; private set; } = 5;

    [DataField("result")]
    public EntProtoId Result;

    [DataField("ingredient")]
    public EntProtoId Ingredient;

    public string Name => Loc.GetString(_name);
}
