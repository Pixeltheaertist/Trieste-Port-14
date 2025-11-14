using Content.Server.Power.EntitySystems;
using Content.Shared._TP.Kitchen.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._TP.Kitchen.Systems;

/// <summary>
///     Makes an entity... make coffee. Idunno how else to describe this.
/// </summary>
public sealed class CoffeeMakerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly List<ReagentId> _coffeeGroundIDs =
    [
        new("TP14ReagentGroundCoffee", null),
        new("TP14ReagentMixedCoffee", null),
        new("TP14ReagentTamperedCoffee", null),
        new("TP14ReagentMixedTamperedCoffee", null),
    ];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedCoffeeMakerComponent, InteractHandEvent>(OnInteractHands);
        SubscribeLocalEvent<SharedCoffeeMakerComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<SharedCoffeeMakerComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<SharedCoffeeMakerComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
    }

    private void OnEntInserted(Entity<SharedCoffeeMakerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        switch (args.Container.ID)
        {
            case "Beaker":
                _appearance.SetData(ent.Owner, CoffeeMakerVisuals.Pitcher, true);
                break;
            case "Basket":
                _appearance.SetData(ent.Owner, CoffeeMakerVisuals.Basket, true);
                break;
        }
    }

    private void OnEntRemoved(Entity<SharedCoffeeMakerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        switch (args.Container.ID)
        {
            case "Beaker":
                if (ent.Comp.IsEnabled)
                    return;

                _appearance.SetData(ent.Owner, CoffeeMakerVisuals.Pitcher, false);
                break;
            case "Basket":
                if (ent.Comp.IsEnabled)
                    return;

                _appearance.SetData(ent.Owner, CoffeeMakerVisuals.Basket, false);
                break;
        }
    }

    /// <summary>
    ///     Verbs for the coffee maker.
    /// </summary>
    /// <param name="ent">CoffeeMaker entity</param>
    /// <param name="args">AlternativeVerb arguments</param>
    private void OnAlternativeVerb(Entity<SharedCoffeeMakerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!ent.Comp.IsEnabled)
        {
            var user = args.User;
            var verb = new AlternativeVerb
            {
                Text = Loc.GetString("coffee-maker-verb-enable"),
                Act = () => AttemptEnable(ent, user),
            };

            args.Verbs.Add(verb);
        }
    }

    /// <summary>
    ///     Called when we want to attempt to enable the coffee maker.
    /// </summary>
    /// <param name="ent">CoffeeMaker entity</param>
    /// <param name="user">User uid</param>
    private void AttemptEnable(Entity<SharedCoffeeMakerComponent> ent, EntityUid user)
    {
        // First, we check for each container if it has items in it.
        // If any of them return null or empty, we return and display a popup message.
        if (!_container.TryGetContainer(ent.Owner, "Beaker", out var beakerContainer) || beakerContainer.ContainedEntities.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("coffee-maker-message-no-beaker"), ent.Owner, user);
            return;
        }

        if (!_container.TryGetContainer(ent.Owner, "Basket", out var basketContainer) || basketContainer.ContainedEntities.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("coffee-maker-message-no-basket"), ent.Owner, user);
            return;
        }

        if (!_container.TryGetContainer(ent.Owner, "Filter", out var filterContainer) || filterContainer.ContainedEntities.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("coffee-maker-message-no-filter"), ent.Owner, user);
            return;
        }

        // Another special case for the coffee filters, for if they're dirty.
        var filterEnt = filterContainer.ContainedEntities[0];
        if (MetaData(filterEnt).EntityPrototype?.ID == "TP14CoffeeFilterDirty")
        {
            _popup.PopupEntity(Loc.GetString("coffee-maker-message-dirty-filter"), ent.Owner, user);
        }

        // Then we check if the basket has any grounds in it.
        // If not, we return early.
        var basketEnt = basketContainer.ContainedEntities[0];
        if (!_solution.TryGetSolution(basketEnt, "food", out var basketSolution, out var basketSolutionComp))
        {
            _popup.PopupEntity(Loc.GetString("coffee-maker-message-no-grounds"), ent.Owner, user);
            return;
        }

        // Now we check the grounds in the basket, and if it's empty, we return early.
        var hasGrounds = false;
        foreach (var groundId in _coffeeGroundIDs)
        {
            var groundAmount = basketSolutionComp.GetReagentQuantity(groundId);
            if (groundAmount > 0)
            {
                hasGrounds = true;
                break;
            }
        }

        if (!hasGrounds)
        {
            _popup.PopupEntity(Loc.GetString("coffee-maker-message-no-grounds"), ent.Owner, user);
            return;
        }

        // And finally, before finishing up properly, we check if it's powered.
        if (!_power.IsPowered(ent))
        {
            _popup.PopupEntity(Loc.GetString("coffee-maker-message-unpowered"), ent.Owner, user);
            return;
        }

        ent.Comp.IsEnabled = true;
        ent.Comp.StartTime = _timing.CurTime;
        _popup.PopupEntity(Loc.GetString("coffee-maker-message-enabled"), ent.Owner, user);
    }

    /// <summary>
    ///     Called when the user interacts with the coffee maker with their hands.
    /// </summary>
    /// <param name="ent">CoffeeMaker entity</param>
    /// <param name="args">InteractHand arguments</param>
    private void OnInteractHands(Entity<SharedCoffeeMakerComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        // This was originally for ejecting items, but turns out we don't need this.
        // This just tells you when it's brewing now, lol.
        if (!ent.Comp.IsEnabled)
            return;

        _popup.PopupEntity(Loc.GetString("coffee-maker-message-brewing"), ent.Owner, args.User);
        args.Handled = true;
        return;
    }

    /// <summary>
    ///     The main update loop for the coffee maker.
    /// </summary>
    /// <param name="frameTime">Current frame timing (I guess?)</param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SharedCoffeeMakerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // If the coffee maker isn't enabled we don't do anything.
            // Then we move onto container checking (again!), plus returning their first contained entity.
            if (!comp.IsEnabled)
                continue;

            if (!_container.TryGetContainer(uid, "Beaker", out var beakerContainer) || beakerContainer.ContainedEntities.Count == 0)
            {
                comp.IsEnabled = false;
                continue;
            }

            if (!_container.TryGetContainer(uid, "Basket", out var basketContainer) || basketContainer.ContainedEntities.Count == 0)
            {
                comp.IsEnabled = false;
                continue;
            }

            if (!_container.TryGetContainer(uid, "Filter", out var filterContainer) || filterContainer.ContainedEntities.Count == 0)
            {
                comp.IsEnabled = false;
                continue;
            }

            var beakerEnt = beakerContainer.ContainedEntities[0];
            var basketEnt = basketContainer.ContainedEntities[0];
            var filterEnt = filterContainer.ContainedEntities[0];

            // Now we increase the internal (fake) temperature of the coffee maker.
            // If it reaches 195 degrees, we handle the brewing process.
            comp.CurrentHeat += comp.HeatingAmount * frameTime;
            if (comp.CurrentHeat >= 195f)
            {
                // First we get the solutions of the basket and beaker
                // Afterward, we remove the grounds from the basket and add a coffee reagent to the beaker.
                // These depend on the grounds used.
                // Normal - Coffee, Tampered/Mixed - Bad Coffee, Mixed AND Tampered - Quality Coffee.
                if (!_solution.TryGetSolution(basketEnt, "food", out var basketSolution))
                {
                    comp.IsEnabled = false;
                    continue;
                }

                if (!_solution.TryGetSolution(beakerEnt, "drink", out var beakerSolution))
                {
                    comp.IsEnabled = false;
                    continue;
                }

                foreach (var groundId in _coffeeGroundIDs)
                {
                    // Ground removal and coffee adding processes
                    var groundsAmount = basketSolution.Value.Comp.Solution.GetReagentQuantity(groundId);
                    if (groundsAmount <= 0)
                        continue;

                    _solution.RemoveReagent(basketSolution.Value, groundId, groundsAmount);

                    if (groundId.Prototype == "TP14ReagentGroundCoffee")
                    {
                        _solution.TryAddReagent(beakerSolution.Value, "Coffee", groundsAmount * 2, out _);
                    }

                    if (groundId.Prototype == "TP14ReagentMixedCoffee" ||
                        groundId.Prototype == "TP14ReagentTamperedCoffee")
                    {
                        _solution.TryAddReagent(beakerSolution.Value, "TP14ReagentBadCoffee", groundsAmount * 2, out _);
                    }

                    if (groundId.Prototype == "TP14ReagentMixedTamperedCoffee")
                    {
                        _solution.TryAddReagent(beakerSolution.Value, "TP14ReagentQualityCoffee", groundsAmount * 2, out _);
                    }
                }

                // Filter handling
                // If the filter is normal, we dirty it. Otherwise, we continue.
                if (TryComp<CoffeeFilterComponent>(filterEnt, out var filterComp))
                {
                    if (!filterComp.InfiniteUses)
                    {
                        _container.Remove(filterEnt, filterContainer);
                        QueueDel(filterEnt);

                        var dirtyEnt = Spawn("TP14CoffeeFilterDirty", Transform(uid).Coordinates);
                        _container.Insert(dirtyEnt, filterContainer);
                    }
                }

                // Power-outage handling - We just turn it off and set the heat to 0.
                if (!_power.IsPowered(uid))
                {
                    comp.IsEnabled = false;
                    comp.CurrentHeat = 0f;
                }

                // Finally, we reset the maker and display a popup message.
                comp.IsEnabled = false;
                comp.CurrentHeat = 0f;
                _popup.PopupEntity(Loc.GetString("coffee-maker-message-complete"), uid);
            }
        }
    }
}
