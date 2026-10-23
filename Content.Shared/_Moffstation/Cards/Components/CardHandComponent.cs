using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Cards.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CardHandSystem))]
public sealed partial class CardHandComponent : CardStackComponent
{
    [DataField]
    public float Angle = 120f;

    [DataField]
    public float XOffset = 0.5f;

    [DataField]
    public float Scale = 1;

    [DataField]
    public LocId PickACardText = "cards-verb-pickcard";

    [DataField]
    public SpriteSpecifier? PickACardIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/die.svg.192dpi.png"));

    [DataField]
    public LocId ConvertToDeckText = "cards-verb-convert-to-deck";

    [DataField]
    public SpriteSpecifier? ConvertToDeckIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/rotate_cw.svg.192dpi.png"));

    [DataField]
    public LocId CardsAddedText = "cards-stackquantitychange-added";

    [DataField]
    public LocId CardsRemovedText = "cards-stackquantitychange-removed";

    [DataField]
    public LocId CardsChangedText = "cards-stackquantitychange-unknown";

    /// Sprite layers added to this entity based on contained cards' <see cref="CardComponent.CurrentSprite"/>.
    [ViewVariables]
    public HashSet<string> SpriteLayersAdded = [];
}

[Serializable, NetSerializable]
public enum CardUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class CardHandDrawMessage(NetEntity card) : BoundUserInterfaceMessage
{
    public NetEntity Card = card;
}
