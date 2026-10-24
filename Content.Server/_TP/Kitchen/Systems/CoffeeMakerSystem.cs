using Content.Server.Power.EntitySystems;
using Content.Shared._TP.Kitchen.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._TP.Kitchen.Systems;

/// <summary>
///     Makes an entity... make coffee. Idunno how else to describe this.
/// </summary>
public sealed partial class CoffeeMakerSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CoffeeMakerComponent, GetVerbsEvent<ActivationVerb>>(OnActivationVerb);
        SubscribeLocalEvent<CoffeeMakerComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<CoffeeMakerComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<CoffeeMakerComponent, ContainerIsRemovingAttemptEvent>(OnAttemptRemove);
    }

    private void OnAttemptRemove(Entity<CoffeeMakerComponent> ent, ref ContainerIsRemovingAttemptEvent args)
    {
        if (ent.Comp.IsEnabled)
        {
            args.Cancel();
        }
    }

    private void OnEntInserted(Entity<CoffeeMakerComponent> ent, ref EntInsertedIntoContainerMessage args)
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

    private void OnEntRemoved(Entity<CoffeeMakerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        switch (args.Container.ID)
        {
            case "Beaker":
                if (ent.Comp.IsEnabled)
                {
                    _popup.PopupEntity(Loc.GetString("coffee-maker-message-brewing"), ent.Owner);
                    return;
                }

                _appearance.SetData(ent.Owner, CoffeeMakerVisuals.Pitcher, false);
                break;
            case "Basket":
                if (ent.Comp.IsEnabled)
                {
                    _popup.PopupEntity(Loc.GetString("coffee-maker-message-brewing"), ent.Owner);
                    return;
                }

                _appearance.SetData(ent.Owner, CoffeeMakerVisuals.Basket, false);
                break;
        }
    }

    /// <summary>
    ///     Verbs for the coffee maker.
    /// </summary>
    /// <param name="ent">CoffeeMaker entity</param>
    /// <param name="args">AlternativeVerb arguments</param>
    private void OnActivationVerb(Entity<CoffeeMakerComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!ent.Comp.IsEnabled)
        {
            var user = args.User;
            var verb = new ActivationVerb
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
    private void AttemptEnable(Entity<CoffeeMakerComponent> ent, EntityUid user)
    {
        // First, we check for each container if it has items in it.
        // If any of them return null or empty, we return and display a popup message.
        if (!_container.TryGetContainer(ent.Owner, "Beaker", out var beakerContainer) || beakerContainer.ContainedEntities.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("coffee-maker-message-no-beaker"), user, user);
            return;
        }

        if (!_container.TryGetContainer(ent.Owner, "Basket", out var basketContainer) || basketContainer.ContainedEntities.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("coffee-maker-message-no-basket"), user, user);
            return;
        }

        if (!_container.TryGetContainer(ent.Owner, "Filter", out var filterContainer) || filterContainer.ContainedEntities.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("coffee-maker-message-no-filter"), user, user);
            return;
        }

        // Another special case for the coffee filters, for if they're dirty.
        var filterEnt = filterContainer.ContainedEntities[0];
        if (TryComp<CoffeeFilterComponent>(filterEnt, out var filter) && filter.IsDirty)
        {
            _popup.PopupEntity(Loc.GetString("coffee-maker-message-dirty-filter"), user, user);
            return;
        }

        // Then we check if the basket has any grounds in it.
        // If not, we return early.
        var basketEnt = basketContainer.ContainedEntities[0];
        if (!_solution.TryGetSolution(basketEnt, "food", out _, out var basketSolutionComp))
        {
            _popup.PopupEntity(Loc.GetString("coffee-maker-message-no-grounds"), user, user);
            return;
        }

        // Now we check the grounds in the basket.
        // If any grounds this maker can brew are present, we set a var to true.
        // Otherwise, we return early.
        var hasGrounds = false;
        foreach (var ground in ent.Comp.Brews.Keys)
        {
            var groundAmount = basketSolutionComp.GetReagentQuantity(new ReagentId(ground, null));
            if (groundAmount > 0)
            {
                hasGrounds = true;
                break;
            }
        }

        if (!hasGrounds)
        {
            _popup.PopupEntity(Loc.GetString("coffee-maker-message-no-grounds"), user, user);
            return;
        }

        // And finally, before finishing up properly, we check if it's powered.
        if (!_power.IsPowered(ent))
        {
            _popup.PopupEntity(Loc.GetString("coffee-maker-message-unpowered", ("entity", ent.Owner)), user, user);
            return;
        }

        ent.Comp.IsEnabled = true;
        ent.Comp.StartTime = _timing.CurTime;
        _popup.PopupEntity(Loc.GetString("coffee-maker-message-enabled", ("entity", ent.Owner)), ent.Owner, user);
        _popup.PopupEntity(Loc.GetString("coffee-maker-message-enabled-other",
                ("user", user),
                ("entity", ent.Owner)),
            ent.Owner,
            Filter.PvsExcept(user),
            true);
    }

    /// <summary>
    ///     The main update loop for the coffee maker.
    /// </summary>
    /// <param name="frameTime">Current frame timing (I guess?)</param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CoffeeMakerComponent>();
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
            // If it reaches the brew temperature, we handle the brewing process.
            comp.CurrentHeat += comp.HeatingAmount * frameTime;
            if (comp.CurrentHeat >= comp.BrewTemperature)
            {
                // First we get the solutions of the basket and beaker
                // Afterward, we remove the grounds from the basket and add a coffee reagent to the beaker.
                // These depend on the grounds used, see Brews in the CoffeeMaker prototype.
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

                foreach (var (ground, brew) in comp.Brews)
                {
                    // Ground removal and coffee adding processes
                    var groundId = new ReagentId(ground, null);
                    var groundsAmount = basketSolution.Value.Comp.Solution.GetReagentQuantity(groundId);
                    if (groundsAmount <= 0)
                        continue;

                    _solution.RemoveReagent(basketSolution.Value, groundId, groundsAmount);
                    _solution.TryAddReagent(beakerSolution.Value, new ReagentId(brew, null), groundsAmount * comp.BrewMultiplier, out _);
                }

                // Power-outage handling - We just turn it off and set the heat to 0.
                if (!_power.IsPowered(uid))
                {
                    comp.IsEnabled = false;
                    comp.CurrentHeat = 0f;
                }

                // Finally, we reset the maker, play a sound, and display a popup message.
                comp.IsEnabled = false;
                comp.CurrentHeat = 0f;
                _audio.PlayPvs(comp.FinishSound, uid, AudioParams.Default.WithVolume(-3));
                _popup.PopupEntity(Loc.GetString("coffee-maker-message-complete"), uid, PopupType.Medium);

                // Filter handling
                // I lied, FINALLY if the filter can get dirty, we swap it for its dirty version. Otherwise, we continue.
                if (TryComp<CoffeeFilterComponent>(filterEnt, out var filterComp)
                    && !filterComp.InfiniteUses
                    && filterComp.DirtyPrototype is { } dirtyProto)
                {
                    _container.Remove(filterEnt, filterContainer);
                    QueueDel(filterEnt);

                    var dirtyEnt = Spawn(dirtyProto, Transform(uid).Coordinates);
                    _container.Insert(dirtyEnt, filterContainer);
                }
            }
        }
    }
}
