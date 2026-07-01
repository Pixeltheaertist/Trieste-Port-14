using Content.Server._TP.Speech.EntitySystems;

namespace Content.Server._TP.Speech.Components;

[RegisterComponent]
[Access(typeof(ChristmasAccentSystem))]
public sealed partial class ChristmasAccentComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("PrefixAndSuffixChance")]
    public float PrefixAndSuffixChance = 0.5f;

    [ViewVariables]
    public readonly List<string> ChristmasWords = new()
    {
        "accent-christmas-prefix-1",
        "accent-christmas-prefix-2",
        "accent-christmas-suffix-1",
        "accent-christmas-suffix-2",
    };
}

