using Content.Shared.Item;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._TP.Kitchen;

/// <summary>
///     Lets the owner entity 'deepfry' items.
///     Created by Cookie (FatherCheese) for Trieste Port 14.
/// </summary>
[RegisterComponent]
[ComponentProtoName("DeepFryer")]
public sealed partial class SharedDeepFryerComponent : Component
{
    [DataField]
    public bool IsEnabled = false;

    [ViewVariables]
    public bool IsBroken;

    [DataField]
    public float CookTimePerLevel = 15.0F;

    [DataField]
    public ProtoId<ItemSizePrototype> MaxItemSize = "Normal";

    [DataField]
    public SoundPathSpecifier FryingSound = new("/Audio/_TP/Machines/Kitchen/frying_idle.ogg");

    [DataField]
    public SoundPathSpecifier Buzzer = new("/Audio/_TP/Machines/Kitchen/frying_buzzer.ogg");

    public string ContainerId = "fryer_slots";

    public string SolutionContainerId = "fryer";
}
