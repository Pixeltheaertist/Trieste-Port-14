using Content.Shared._Moffstation.Cards.Components;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;

namespace Content.Shared._Moffstation.Cards;

public sealed class CardSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly CardHandSystem _cardHand = default!;
    [Dependency] private readonly CardStackSystem _cardStack = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CardComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CardComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<CardComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CardComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<CardComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnStartup(Entity<CardComponent> entity, ref ComponentStartup args)
    {
        _appearance.SetData(entity, CardVisuals.IsFaceDown, entity.Comp.IsFaceDown);
    }

    private void OnExamined(Entity<CardComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || entity.Comp.IsFaceDown)
            return;

        args.PushMarkup(Loc.GetString("card-examined", ("target", Loc.GetString(entity.Comp.Name))));
    }

    private void OnGetVerbs(Entity<CardComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => Flip(entity, faceDown: null),
            Text = Loc.GetString("cards-verb-flip"),
            Icon = entity.Comp.FlipIcon,
            Priority = 1,
        });
    }

    private void OnUse(Entity<CardComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        Flip(entity, faceDown: null);
        args.Handled = true;
    }

    private void OnInteractUsing(Entity<CardComponent> entity, ref InteractUsingEvent args)
    {
        if (_cardStack.TryComp(args.Used, out var usedStack))
        {
            _cardStack.InsertCard((Entity<CardStackComponent>)(args.Used, usedStack), entity);
            args.Handled = true;
            return;
        }

        if (TryComp<CardComponent>(args.Used, out var usedCard) &&
            _cardHand.CreateHand([(args.Used, usedCard), entity]) is { } hand)
        {
            _hands.PickupOrDrop(args.User, hand);
            args.Handled = true;
            return;
        }
    }

    public void Flip(Entity<CardComponent> entity, bool? faceDown)
    {
        var newState = faceDown ?? !entity.Comp.IsFaceDown;
        if (newState != entity.Comp.IsFaceDown)
        {
            var ev = new ContainedCardFlippedEvent();
            RaiseLocalEvent(Transform(entity).ParentUid, ref ev);
        }

        entity.Comp.IsFaceDown = newState;
        _appearance.SetData(entity, CardVisuals.IsFaceDown, newState);
        Dirty(entity);
    }
}
