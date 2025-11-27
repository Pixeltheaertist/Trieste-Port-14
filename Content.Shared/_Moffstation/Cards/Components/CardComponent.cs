using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Cards.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(CardSystem))]
public sealed partial class CardComponent : Component
{
    /// The sprite layers of the face or front of the card.
    [DataField(required: true)]
    public PrototypeLayerData[] ObverseSprite;

    /// The sprite layers of the back of the card.
    [DataField(required: true)]
    public PrototypeLayerData[] ReverseSprite;

    /// <see cref="ObverseSprite"/> or <see cref="ReverseSprite"/>, depending on <see cref="IsFaceDown"/>.
    public PrototypeLayerData[] CurrentSprite => IsFaceDown ? ReverseSprite : ObverseSprite;

    /// Sprite layers added to this entity based on <see cref="CurrentSprite"/>.
    [ViewVariables]
    public HashSet<string> SpriteLayersAdded = [];

    /// Is the card facing down, ie. which side is visible. If true, the <see cref="ReverseSprite"/> is visible.
    [DataField, AutoNetworkedField]
    public bool IsFaceDown;

    /// The name of this card.
    [DataField(required: true), AutoNetworkedField]
    public LocId Name;

    /// The icon for the "flip" verb on this card.
    [DataField]
    public SpriteSpecifier? FlipIcon =
        new SpriteSpecifier.Texture(new ResPath("Interface/VerbIcons/flip.svg.192dpi.png"));
}

[ByRefEvent]
public record struct ContainedCardFlippedEvent;

[Serializable, NetSerializable]
public enum CardVisuals : sbyte
{
    IsFaceDown,
}
