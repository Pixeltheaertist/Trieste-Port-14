using System.Linq;
using Content.Shared._Moffstation.Cards.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Cards;

public sealed class CardHandSystem : CardStackSystem<CardHandComponent>
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private static readonly EntProtoId<CardHandComponent> CardHandEntId = "CardHandBase";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CardHandComponent, CardHandDrawMessage>(OnCardDraw);
    }

    public IEnumerable<Entity<CardComponent>> GetCards(Entity<CardHandComponent?> entity)
    {
        return Resolve(entity, ref entity.Comp) ? GetCards<CardHandComponent>((entity, entity.Comp)) : [];
    }

    protected override void OnCardStackQuantityChange(
        Entity<CardHandComponent> entity,
        ref CardStackQuantityChangeEvent args
    )
    {
        base.OnCardStackQuantityChange(entity, ref args);

        _popup.PopupPredicted(
            Loc.GetString(
                args.Type switch
                {
                    StackQuantityChangeType.Added => entity.Comp.CardsAddedText,
                    StackQuantityChangeType.Removed => entity.Comp.CardsRemovedText,
                    _ => entity.Comp.CardsChangedText,
                },
                ("quantity", entity.Comp.NumCards)
            ),
            entity,
            args.User
        );
    }

    private void OnCardDraw(Entity<CardHandComponent> entity, ref CardHandDrawMessage args)
    {
        var cardEnt = GetEntity(args.Card);
        CardComponent? cardComp = null;
        if (!Resolve(cardEnt, ref cardComp))
            return;
        var card = new Entity<CardComponent>(cardEnt, cardComp);

        CardStack.RemoveCard(entity, card, args.Actor);
        _hands.TryPickupAnyHand(args.Actor, card, animate: false);
        CheckDegenerate(entity);
    }

    protected override void OnGetAlternativeVerbs(
        Entity<CardHandComponent> entity,
        ref GetVerbsEvent<AlternativeVerb> args
    )
    {
        base.OnGetAlternativeVerbs(entity, ref args);

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => _ui.OpenUi(entity.Owner, CardUiKey.Key, user),
            Text = Loc.GetString(entity.Comp.PickACardText),
            Icon = entity.Comp.PickACardIcon,
            Priority = 3,
        });
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => ConvertToDeck(entity, user),
            Text = Loc.GetString(entity.Comp.ConvertToDeckText),
            Icon = entity.Comp.ConvertToDeckIcon,
            Priority = 2,
        });
    }

    private void ConvertToDeck(Entity<CardHandComponent> hand, EntityUid? user)
    {
        var spawned = PredictedSpawnAtPosition(CardDeckSystem.CardDeckEntId, Transform(hand).Coordinates);

        var wasHoldingBeforeConversion = false;
        if (user is { } u && _hands.IsHolding(u, hand))
        {
            wasHoldingBeforeConversion = true;
            // It's gonna get deleted anyway, so drop it so that we can pick up the spawned deck immediately.
            _hands.TryDrop(u, hand, checkActionBlocker: false, doDropInteraction: false);
        }

        if (!IsClientSide(spawned))
        {
            // Server can insert the cards into the deck.
            var deck = new Entity<CardDeckComponent>(spawned, Comp<CardDeckComponent>(spawned));
            TransferImpl(hand, deck, base.GetCards(hand));
        }

        if (wasHoldingBeforeConversion)
        {
            _hands.TryPickupAnyHand(user!.Value, spawned);
        }
    }

    /// Creates a new hand from the given cards. Returns null if no cards were given. Returns an entity which may
    /// be predicted.
    public EntityUid? CreateHand(IEnumerable<Entity<CardComponent>> cards)
    {
        var cardsList = cards.ToList();
        if (cardsList.Count == 0)
            return null;

        var spawned = PredictedSpawnAtPosition(CardHandEntId, Transform(cardsList.First()).Coordinates);
        if (!IsClientSide(spawned))
        {
            // Can't insert cards into a predicted hand.
            var hand = new Entity<CardHandComponent>(spawned, Comp<CardHandComponent>(spawned));
            CardStack.InsertCards(hand, cardsList);
            Card.Flip(base.GetCards(hand), false);
        }

        return spawned;
    }
}
