using Content.Shared.Chemistry.Components;
using Content.Shared.Item;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._TP.Kitchen.Components;

/// <summary>
///     Lets the owner entity 'deepfry' items.
///     Created by Cookie (FatherCheese) for Trieste Port 14.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class DeepFryerComponent : Component
{
    [DataField]
    public bool IsEnabled;

    [ViewVariables]
    public bool IsBroken;

    [DataField]
    public float CookTimePerLevel = 15.0F;

    [DataField]
    public ProtoId<ItemSizePrototype> MaxItemSize = "Huge";

    [DataField]
    public SoundPathSpecifier FryingSound = new("/Audio/_TP/Machines/Kitchen/frying_idle.ogg");

    [DataField]
    public SoundPathSpecifier Buzzer = new("/Audio/_TP/Machines/Kitchen/frying_buzzer.ogg");

    /// <summary>
    /// ID of the container where the foodses will be stored.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ContainerId = "fryer_slots";

    [ViewVariables]
    public ContainerSlot FryerContainer = default!;

    /// <summary>
    /// The name of <see cref="Solution"/>.
    /// </summary>
    [DataField]
    public string SolutionId = "solution";
}

[Serializable, NetSerializable]
public enum DeepFryerVisuals : byte
{
    Base,
    Active,
}
