using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Cards.Components;

/// Holds entityUids of cards in a hand or deck.
[Access(typeof(CardStackSystem))]
// It's abstract, I dunno what you want from me.
#pragma warning disable RXN0007
public abstract partial class CardStackComponent : Component
#pragma warning restore RXN0007
{
    [DataField]
    public SoundSpecifier ShuffleSound = new SoundCollectionSpecifier("cardFan");

    [DataField]
    public SoundSpecifier PickUpSound = new SoundCollectionSpecifier("cardSlide");

    [DataField]
    public SoundSpecifier PlaceDownSound = new SoundCollectionSpecifier("cardShove");

    [DataField]
    public string ContainerId = "cardstack-container";

    /// <summary>
    /// The containers that contain the items held in the stack
    /// </summary>
    [ViewVariables]
    public Container ItemContainer = default!;

    [ViewVariables]
    public int NumCards => NetCards.Count;

    /// <summary>
    /// The list EntityUIds of Cards
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<NetEntity> NetCards = [];

    public bool DirtyVisuals = true;

    [DataField(required: true)]
    public int Limit;

    [DataField]
    public LocId JoinText = "card-verb-join";

    [DataField]
    public SpriteSpecifier? JoinIcon =
        new SpriteSpecifier.Texture(new ResPath("Interface/VerbIcons/refresh.svg.192dpi.png"));
}

[ByRefEvent]
public record struct CardStackQuantityChangeEvent(StackQuantityChangeType Type, EntityUid? User);

[Serializable, NetSerializable]
public enum StackQuantityChangeType : sbyte
{
    Added,
    Removed,
    Joined,
}

[Serializable, NetSerializable]
public enum CardStackVisuals : sbyte
{
    /// This key for appearance data indicates which cards are visible in a stack. It is expected to key a value of type
    /// <c>NetEntity[]</c>.
    Cards,
}
