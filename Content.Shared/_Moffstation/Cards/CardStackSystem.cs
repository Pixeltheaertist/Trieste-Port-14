using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._Moffstation.Cards.Components;
using Content.Shared.Containers;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._Moffstation.Cards;

[Virtual] // Can be instantiated on its own to provide an API for dealing with cards stacks.
public partial class CardStackSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly CardSystem Card = default!;
    [Dependency] protected readonly CardStackSystem CardStack = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] protected readonly SharedStorageSystem Storage = default!;

    public void InsertCard<T>(Entity<T> entity, Entity<CardComponent> card, EntityUid? user = null)
        where T : CardStackComponent
    {
        InsertCards(entity, [card], user);
    }

    public void InsertCards<T>(Entity<T> entity, IEnumerable<Entity<CardComponent>> cards, EntityUid? user = null)
        where T : CardStackComponent
    {
        EntityUid? firstCard = null;
        foreach (var card in cards)
        {
            firstCard ??= card;
            Container.Insert(card.Owner, entity.Comp.ItemContainer);
            entity.Comp.NetCards.Add(GetNetEntity(card));
        }

        if (user is not null && firstCard is { } fc)
        {
            Audio.PlayPredicted(entity.Comp.PlaceDownSound, Transform(entity).Coordinates, user);
            Storage.PlayPickupAnimation(fc, Transform(user.Value).Coordinates, Transform(entity).Coordinates, 0, user);
        }

        Dirty(entity);

        var ev = new CardStackQuantityChangeEvent(StackQuantityChangeType.Added, user);
        RaiseLocalEvent(entity, ref ev);
    }

    public void RemoveCard<T>(Entity<T> entity, Entity<CardComponent> card, EntityUid? user = null)
        where T : CardStackComponent
    {
        RemoveCards(entity, [card], user);
    }

    public void RemoveCards<T>(Entity<T> entity, IEnumerable<Entity<CardComponent>> cards, EntityUid? user = null)
        where T : CardStackComponent
    {
        EntityUid? firstCard = null;
        foreach (var card in cards)
        {
            firstCard ??= card;
            Container.Remove(card.Owner, entity.Comp.ItemContainer);
            entity.Comp.NetCards.Remove(GetNetEntity(card));
        }

        if (user is not null && firstCard is { } fc)
        {
            Audio.PlayPredicted(entity.Comp.PickUpSound, Transform(entity).Coordinates, user);
            Storage.PlayPickupAnimation(fc, Transform(entity).Coordinates, Transform(user.Value).Coordinates, 0, user);
        }

        Dirty(entity);

        var ev = new CardStackQuantityChangeEvent(StackQuantityChangeType.Removed, user);
        RaiseLocalEvent(entity, ref ev);

        CheckDegenerate(entity);
    }

    public void TransferCards<TFrom, TTo>(
        Entity<TFrom> from,
        Entity<TTo> to,
        IEnumerable<Entity<CardComponent>> cards,
        EntityUid? user = null
    )
        where TFrom : CardStackComponent where TTo : CardStackComponent
    {
        var cardsList = cards.ToList();
        TransferImpl(from, to, cardsList);

        var ev = new CardStackQuantityChangeEvent(StackQuantityChangeType.Removed, user);
        RaiseLocalEvent(from, ref ev);

        var ev1 = new CardStackQuantityChangeEvent(StackQuantityChangeType.Added, user);
        RaiseLocalEvent(to, ref ev1);

        if (user is not null)
        {
            Audio.PlayPredicted(to.Comp.PlaceDownSound, Transform(to).Coordinates, user);
            Storage.PlayPickupAnimation(
                cardsList.Count == 1 ? cardsList.First() : from,
                Transform(from).Coordinates,
                Transform(to).Coordinates,
                0,
                user
            );
        }
    }

    protected void TransferImpl<TFrom, TTo>(
        Entity<TFrom> from,
        Entity<TTo> to,
        IEnumerable<Entity<CardComponent>> cards
    ) where TFrom : CardStackComponent where TTo : CardStackComponent
    {
        foreach (var card in cards.ToList())
        {
            Container.Remove(card.Owner, from.Comp.ItemContainer);
            from.Comp.NetCards.Remove(GetNetEntity(card));
            Container.Insert(card.Owner, to.Comp.ItemContainer);
            to.Comp.NetCards.Add(GetNetEntity(card));
        }

        Dirty(to);
        Dirty(from);

        CheckDegenerate(from);
    }

    /// Tries to get a <see cref="CardStackComponent"/> on the given <paramref name="uid"/>. This is needed because
    /// <see cref="CardStackComponent"/> is abstract, so we can query it like normal. This of this like a factory
    /// function.
    public bool TryComp([NotNullWhen(true)] EntityUid? uid, [NotNullWhen(true)] out CardStackComponent? comp)
    {
        if (TryComp<CardDeckComponent>(uid, out var deck))
        {
            comp = deck;
            return true;
        }

        if (TryComp<CardHandComponent>(uid, out var hand))
        {
            comp = hand;
            return true;
        }

        comp = null;
        return false;
    }

    protected IEnumerable<Entity<CardComponent>> GetCards<T>(Entity<T> entity) where T : CardStackComponent
    {
        var query = GetEntityQuery<CardComponent>();
        foreach (var netCard in entity.Comp.NetCards)
        {
            var cardEnt = GetEntity(netCard);
            if (query.TryComp(cardEnt, out var cardComp))
                yield return (cardEnt, cardComp);
        }
    }

    protected void CheckDegenerate<T>(Entity<T> entity) where T : CardStackComponent
    {
        if (entity.Comp.NumCards > 1)
            return;
        if (entity.Comp.NumCards <= 0)
        {
            PredictedQueueDel(entity);
            return;
        }

        if (entity.Comp is CardHandComponent)
        {
            var lastCard = GetCards(entity).Single();
            CardStack.RemoveCard(entity, lastCard);

            // If the hand was in a container, leave the last card in its place in the container.
            var cardParent = Transform(entity).ParentUid;
            if (Container.TryGetContainingContainer(cardParent, entity, out var container))
            {
                Container.Remove(entity.Owner, container, force: true);
                Container.Insert(lastCard.Owner, container);
            }

            PredictedQueueDel(entity);
        }
    }
}

public abstract class CardStackSystem<TComp> : CardStackSystem where TComp : CardStackComponent
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TComp, ComponentStartup>(OnStartup);
        // Startup after ContainerFill so that we actually have our cards.
        SubscribeLocalEvent<TComp, MapInitEvent>(OnMapInit, after: [typeof(ContainerFillSystem)]);
        SubscribeLocalEvent<TComp, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
        SubscribeLocalEvent<TComp, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<TComp, InteractUsingEvent>(OnInteractUsing);

        // Update visuals when cards in the stack change.
        SubscribeLocalEvent<TComp, CardStackQuantityChangeEvent>(OnCardStackQuantityChange);
        SubscribeLocalEvent<TComp, ContainedCardFlippedEvent>(OnContainedCardFlipped);
    }

    private void OnStartup(Entity<TComp> entity, ref ComponentStartup args)
    {
        entity.Comp.ItemContainer = Container.EnsureContainer<Container>(entity, entity.Comp.ContainerId);
    }

    private void OnMapInit(Entity<TComp> entity, ref MapInitEvent args)
    {
        // Manually synchronize entities in the container to our internal list.
        entity.Comp.NetCards.AddRange(
            GetNetEntityList(
                Container.GetContainer(entity, entity.Comp.ContainerId)
                    .ContainedEntities
            )
        );
        Dirty(entity);
    }

    [MustCallBase]
    protected virtual void OnCardStackQuantityChange(Entity<TComp> entity, ref CardStackQuantityChangeEvent args)
    {
        entity.Comp.DirtyVisuals = true;
    }

    private static void OnContainedCardFlipped(Entity<TComp> entity, ref ContainedCardFlippedEvent args)
    {
        entity.Comp.DirtyVisuals = true;
    }

    private void OnExamine(Entity<TComp> entity, ref ExaminedEvent args)
    {
        args.PushText(Loc.GetString("card-stack-examine", ("count", entity.Comp.NumCards)));
    }

    [MustCallBase]
    protected virtual void OnGetAlternativeVerbs(Entity<TComp> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (args.Using == args.Target ||
            args.Using is not { } usedEnt ||
            !CardStack.TryComp(args.Using, out var usingStack))
            return;

        var user = args.User;
        var usedStack = new Entity<CardStackComponent>(usedEnt, usingStack);

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString(entity.Comp.JoinText),
            Icon = entity.Comp.JoinIcon,
            Priority = 8,
            Act = () => JoinVerbImpl(entity, usedStack, user),
        });
    }

    private void JoinVerbImpl(Entity<TComp> entity, Entity<CardStackComponent> usedStack, EntityUid user)
    {
        TransferImpl(usedStack, entity, GetCards(usedStack));

        var ev = new CardStackQuantityChangeEvent(StackQuantityChangeType.Joined, user);
        RaiseLocalEvent(entity, ref ev);

        Audio.PlayPredicted(usedStack.Comp.PlaceDownSound, Transform(usedStack).Coordinates, user);
        Storage.PlayPickupAnimation(entity, Transform(usedStack).Coordinates, Transform(entity).Coordinates, 0, user);
    }

    private void OnInteractUsing(Entity<TComp> entity, ref InteractUsingEvent args)
    {
        if (TryComp<CardComponent>(args.Used, out var usedCard))
        {
            CardStack.InsertCard(entity, (args.Used, usedCard), args.User);
            args.Handled = true;
            return;
        }

        if (CardStack.TryComp(args.Used, out var usedStack))
        {
            var cardToDraw = GetCards(entity).TakeLast(1).ToList();
            CardStack.TransferCards<TComp, CardStackComponent>(
                entity,
                (args.Used, usedStack),
                cardToDraw,
                args.User
            );
            Card.Flip(cardToDraw, faceDown: false); // Flip drawn card
            args.Handled = true;
            return;
        }
    }

    public override void Update(float frameTime)
    {
        var stacks = EntityQueryEnumerator<TComp>();
        while (stacks.MoveNext(out var ent, out var comp))
        {
            if (!comp.DirtyVisuals ||
                Deleted(ent))
                continue;

            _appearance.SetData(
                ent,
                CardStackVisuals.Cards,
                comp.NetCards[^Math.Min(comp.Limit, comp.NumCards)..].ToArray()
            );
            comp.DirtyVisuals = false;
        }
    }
}
