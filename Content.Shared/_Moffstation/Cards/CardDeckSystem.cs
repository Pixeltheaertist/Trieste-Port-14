using System.Linq;
using Content.Shared._Moffstation.Cards.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Cards;

public sealed class CardDeckSystem : CardStackSystem<CardDeckComponent>
{
    [Dependency] private readonly CardSystem _card = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly AudioParams AudioVariation = AudioParams.Default.WithVariation(0.05f);

    public static readonly EntProtoId<CardDeckComponent> CardDeckEntId = "CardDeckBase";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CardDeckComponent, InteractHandEvent>(OnInteractHand);
    }


    protected override void OnGetAlternativeVerbs(
        Entity<CardDeckComponent> entity,
        ref GetVerbsEvent<AlternativeVerb> args
    )
    {
        base.OnGetAlternativeVerbs(entity, ref args);

        if (!args.CanAccess ||
            !args.CanInteract ||
            args.Hands == null)
            return;

        var user = args.User;

        if (entity.Comp.NumCards > 1)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Act = () => Split(entity, user),
                Text = Loc.GetString("cards-verb-split"),
                Icon = entity.Comp.SplitIcon,
                Priority = 4,
            });
        }

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => Shuffle(entity, user),
            Text = Loc.GetString("cards-verb-shuffle"),
            Icon = entity.Comp.ShuffleIcon,
            Priority = 3,
        });
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => FlipAll(entity, false, user),
            Text = Loc.GetString("cards-verb-organize-up"),
            Icon = entity.Comp.FlipCardsIcon,
            Priority = 1,
        });
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => FlipAll(entity, true, user),
            Text = Loc.GetString("cards-verb-organize-down"),
            Icon = entity.Comp.FlipCardsIcon,
            Priority = 2,
        });
    }

    private void Split(Entity<CardDeckComponent> entity, EntityUid user)
    {
        Audio.PlayPredicted(entity.Comp.PickUpSound, entity, user);

        var spawned = Spawn(CardDeckEntId, Transform(entity).Coordinates);
        if (!IsClientSide(spawned))
        {
            // Can't insert real cards into predicted decks.
            var deck = new Entity<CardDeckComponent>(spawned, Comp<CardDeckComponent>(spawned));
            CardStack.TransferCards(entity, deck, GetCards(entity).TakeLast(entity.Comp.NumCards / 2), user);
        }

        _hands.TryPickupAnyHand(user, spawned);
    }


    private void Shuffle(Entity<CardDeckComponent> entity, EntityUid? user)
    {
        var rand = new System.Random(
            SharedRandomExtensions.HashCodeCombine((int)_gameTiming.CurTick.Value, entity.Owner.Id));
        rand.Shuffle(entity.Comp.NetCards);
        Dirty(entity);
        entity.Comp.DirtyVisuals = true;

        Audio.PlayPredicted(entity.Comp.ShuffleSound, entity, user, AudioVariation);
        _popup.PopupPredicted(Loc.GetString("card-verb-shuffle-success", ("target", MetaData(entity).EntityName)),
            entity,
            user);
    }

    private void FlipAll(Entity<CardDeckComponent> entity, bool isFlipped, EntityUid user)
    {
        Card.Flip(GetCards(entity), isFlipped);

        Audio.PlayPredicted(entity.Comp.ShuffleSound, entity, user, AudioVariation);
    }


    private void OnInteractHand(Entity<CardDeckComponent> entity, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        var card = GetCards(entity).Last();
        CardStack.RemoveCard(entity, card, args.User);
        _hands.TryPickupAnyHand(args.User, card, animate: false);

        args.Handled = true;
    }
}
