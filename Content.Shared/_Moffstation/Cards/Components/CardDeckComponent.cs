using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Cards.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CardDeckSystem))]
public sealed partial class CardDeckComponent : CardStackComponent
{
    [DataField]
    public float YOffset = 0.02f;

    [DataField]
    public float Scale = 1;

    [DataField] public SpriteSpecifier? SplitIcon =
        new SpriteSpecifier.Texture(new ResPath("Interface/VerbIcons/dot.svg.192dpi.png"));

    [DataField] public SpriteSpecifier? ShuffleIcon =
        new SpriteSpecifier.Texture(new ResPath("Interface/VerbIcons/die.svg.192dpi.png"));

    [DataField] public SpriteSpecifier? FlipCardsIcon =
        new SpriteSpecifier.Texture(new ResPath("Interface/VerbIcons/refresh.svg.192dpi.png"));

    /// Sprite layers added to this entity based on contained cards' <see cref="CardComponent.CurrentSprite"/>.
    [ViewVariables]
    public HashSet<string> SpriteLayersAdded = [];
}
