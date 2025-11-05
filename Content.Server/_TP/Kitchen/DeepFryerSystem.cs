using Content.Server.Hands.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._TP.Kitchen;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._TP.Kitchen;


public sealed class DeepFryerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedDeepFryerComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<SharedDeepFryerComponent, InteractUsingEvent>(AfterInteractUsing);
        SubscribeLocalEvent<SharedDeepFryerComponent, ComponentShutdown>(OnShutdown);
    }

    private readonly Dictionary<EntityUid, TimeSpan> _cookingStartTimes = new();
    private readonly Dictionary<EntityUid, EntityUid?> _fryerSounds = new();

    private void OnShutdown(Entity<SharedDeepFryerComponent> ent, ref ComponentShutdown args)
    {
        if (_fryerSounds.TryGetValue(ent, out var soundEntity) && soundEntity != null)
            _audio.Stop(soundEntity.Value);

        _fryerSounds.Remove(ent);
    }

    /// <summary>
    ///     AfterInteractUsing event for the deep fryer.
    ///     We use this here to block interactions, such as the container.
    /// </summary>
    /// <param name="ent">SharedDeepFryerComponent entity</param>
    /// <param name="args">InteractUsingEvent arguments</param>
    private void AfterInteractUsing(Entity<SharedDeepFryerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var usedMeta = MetaData(args.Used);
        if (usedMeta.EntityName.StartsWith("burnt"))
        {
            _popup.PopupEntity("TEMP - You cannot cook a burnt item", ent, args.User);
            args.Handled = true;
            return;
        }


        if (!_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.SolutionContainerId, out _, out var solName))
            return;

        if (solName.Volume <= 25)
        {
            _popup.PopupEntity("TEMP - Deep Fryer has low or no oil", ent, args.User);
            args.Handled = true;
            return;
        }


        if (TryComp<ItemComponent>(args.Used, out var itemComp))
        {
            if (itemComp.Size == "Small" || itemComp.Size == "Tiny" || itemComp.Size != "Large")
                return;

            _popup.PopupEntity("TEMP - You cannot cook a large item", ent, args.User);
            args.Handled = true;
            return;
        }
    }

    private DeepFryingRecipePrototype? FindMatchingRecipe(EntityUid item)
    {
        var itemProto = MetaData(item).EntityPrototype?.ID;
        if (itemProto == null)
            return null;

        foreach (var recipe in _proto.EnumeratePrototypes<DeepFryingRecipePrototype>())
        {
            if (recipe.Ingredient == itemProto)
            {
                return recipe;
            }
        }

        return null;
    }

    /// <summary>
    ///     Hand interactions for the deep fryer.
    /// </summary>
    /// <param name="deepFryerEnt">Deep Fryer Entity UID</param>
    /// <param name="args">InteractHandEvent Arguments</param>
    private void OnInteractHand(Entity<SharedDeepFryerComponent> deepFryerEnt, ref InteractHandEvent args)
    {
        // First, check if the entity has already been handled. If so, return early.
        // Secondly, we add a var for the deep fryer component from the ent.
        if (args.Handled)
            return;

        var deepFryerComp = deepFryerEnt.Comp;

        // Now we check if the deep fryer is powered through APC receiver, and _power.
        // If not, popup a message and return.
        if (!(TryComp<ApcPowerReceiverComponent>(deepFryerEnt, out var apc) && apc.Powered) || !_power.IsPowered(deepFryerEnt))
        {
            _popup.PopupEntity("TEMP - Deep Fryer is not powered", deepFryerEnt, args.User);
            return;
        }

        // Now we check if the deep fryer is broken. If so, popup a message and return.
        if (deepFryerComp.IsBroken)
        {
            _popup.PopupEntity("TEMP - Deep Fryer is broken", deepFryerEnt, args.User);
            return;
        }

        // NOW we check if the deep fryer has enough oil. This is done via Olive Oil for now.
        // This is also done with two checks - a total volume, and specifically olive oil.
        // If either are false, popup and return.
        if (!_solutionContainer.TryGetSolution(deepFryerEnt.Owner, deepFryerComp.SolutionContainerId, out _, out var solName))
            return;

        if (solName.Volume <= 25)
        {
            _popup.PopupEntity("TEMP - Deep Fryer has low or no oil", deepFryerEnt, args.User);
            return;
        }

        var cookingOilAmnt = solName.GetTotalPrototypeQuantity("OilOlive");
        if (cookingOilAmnt <= 25)
        {
            _popup.PopupEntity("TEMP - Deep Fryer has low or no oil", deepFryerEnt, args.User);
            return;
        }

        // One of the last steps! We get the container ID for the deep fryer and the contained entities.
        // For each item, we check if it HAS a recipe. If so, popup a message and return.
        // Otherwise, we remove the item from the container and add it to the player's hand with TWO popups.
        if (!_container.TryGetContainer(deepFryerEnt, deepFryerComp.ContainerId, out var container))
            return;

        foreach (var entity in container.ContainedEntities)
        {
            var recipe = FindMatchingRecipe(entity);
            if (recipe != null)
            {
                _popup.PopupEntity("TEMP - You cannot grab an item while it's cooking", deepFryerEnt, args.User);
                return;
            }

            _popup.PopupEntity("TEMP - You grabbed an item", deepFryerEnt, args.User);
            _popup.PopupEntity("TEMP - PLAYER grabs ITEM from the fryer", deepFryerEnt, Filter.PvsExcept(args.User), true);

            _container.Remove(entity, container);
            _hands.PickupOrDrop(args.User, entity);
        }

        // Otherwise, if the container has NO items, we toggle the deep fryer with two popups.
        if (container.ContainedEntities.Count == 0)
        {
            _popup.PopupEntity(deepFryerComp.IsEnabled
                ? "TEMP - You turn the deep fryer off"
                : "TEMP - You turn the deep fryer on",
            deepFryerEnt,
            args.User);

            _popup.PopupEntity(deepFryerComp.IsEnabled
                ? "TEMP - PLAYER turns the deep fryer off"
                : "TEMP - PLAYER turns the deep fryer on",
            deepFryerEnt,
            Filter.PvsExcept(args.User),
            true);

            if (deepFryerComp.IsEnabled)
            {
                if (_fryerSounds.TryGetValue(deepFryerEnt, out var soundEntity) && soundEntity != null)
                    _audio.Stop(soundEntity.Value);

                _fryerSounds.Remove(deepFryerEnt);
            }

            deepFryerComp.IsEnabled = !deepFryerComp.IsEnabled;
        }

        args.Handled = true;
    }

    /// <summary>
    ///     The main update loop for the deep fryer.
    /// </summary>
    /// <param name="frameTime"></param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // We start by getting all the active deep fryers in the server.
        var query = EntityQueryEnumerator<SharedDeepFryerComponent>();
        while (query.MoveNext(out var uid, out var deepFryerComp))
        {
            // If the comp isn't enabled, or it isn't powered, don't do anything.
            // If both are enabled, we play a looping, idle frying sound.
            // It's not really a deep-fryer sound, but shhh!
            if (!deepFryerComp.IsEnabled)
                continue;

            if (!_power.IsPowered(uid))
                continue;

            if (deepFryerComp.IsEnabled && _power.IsPowered(uid))
            {
                if (!_fryerSounds.ContainsKey(uid) || _fryerSounds[uid] == null)
                {
                    var sound = _audio.PlayPvs(deepFryerComp.FryingSound, uid, AudioParams.Default.WithLoop(true).WithVolume(-3));
                    _fryerSounds[uid] = sound?.Entity;
                }
            }
            else
            {
                if (_fryerSounds.TryGetValue(uid, out var soundEntity) && soundEntity != null)
                {
                    _audio.Stop(soundEntity.Value);
                    _fryerSounds[uid] = null;
                }
            }

            // Now we check for if the deep fryer has enough oil. If not, disable it and skip the loop.
            if (!_solutionContainer.TryGetSolution(uid,
                    deepFryerComp.SolutionContainerId,
                    out _,
                    out var solName))
                continue;

            var cookingOilAmnt = solName.GetTotalPrototypeQuantity("OilOlive");
            if (cookingOilAmnt <= 25 || solName.Volume <= 25)
            {
                deepFryerComp.IsEnabled = false;
                continue;
            }

            // Now we check for if it's a container. If not, skip the loop.
            // But for each item in an active fryer we set a timer, and we check if it's a recipe.
            if (!_container.TryGetContainer(uid, deepFryerComp.ContainerId, out var container))
                continue;

            if (container.ContainedEntities.Count == 0)
                continue;

            foreach (var entity in container.ContainedEntities)
            {
                if (!_cookingStartTimes.ContainsKey(entity))
                {
                    _cookingStartTimes[entity] = _timing.CurTime;
                }

                // Assuming the item is a recipe, we then get the recipe's cook time.
                // If it's elapsed enough, we delete the recipe item and replace it with the result.
                var recipe = FindMatchingRecipe(entity);
                if (recipe != null)
                {
                    if (_cookingStartTimes.TryGetValue(entity, out var startTime))
                    {
                        var elapsed = _timing.CurTime - startTime;
                        if (elapsed.TotalSeconds >= recipe.CookTime)
                        {
                            _container.Remove(entity, container);
                            QueueDel(entity);

                            var recipeResult = Spawn(recipe.Result, Transform(uid).Coordinates);
                            _container.Insert(recipeResult, container);

                            _cookingStartTimes.Remove(entity);

                            _audio.PlayPvs(deepFryerComp.Buzzer, uid, AudioParams.Default.WithVolume(-5));
                        }
                    }
                }
                else
                {
                    var itemMeta = MetaData(entity);

                    if (!_cookingStartTimes.ContainsKey(entity))
                    {
                        _cookingStartTimes[entity] = _timing.CurTime;
                    }

                    if (_cookingStartTimes.TryGetValue(entity, out var startTime))
                    {
                        var elapsed = _timing.CurTime - startTime;
                        if (elapsed.TotalSeconds >= deepFryerComp.CookTimePerLevel)
                        {
                            EnsureComp<SharedDeepFriedComponent>(entity, out var deepFriedComp);

                            _cookingStartTimes.Remove(entity);

                            if (itemMeta.EntityName.StartsWith("lightly-fried"))
                            {
                                _metaData.SetEntityName(entity, itemMeta.EntityName.Replace("lightly-fried", "fried"));
                                deepFriedComp.CurrentFriedLevel = SharedDeepFriedComponent.FriedLevel.Fried;
                            }
                            else if (itemMeta.EntityName.StartsWith("fried"))
                            {
                                _metaData.SetEntityName(entity, itemMeta.EntityName.Replace("fried", "burnt"));
                                deepFriedComp.CurrentFriedLevel = SharedDeepFriedComponent.FriedLevel.Burnt;
                                _container.InsertOrDrop(entity, container);
                            }
                            else
                            {
                                _metaData.SetEntityName(entity, itemMeta.EntityName.Insert(0, "lightly-fried "));
                                deepFriedComp.CurrentFriedLevel = SharedDeepFriedComponent.FriedLevel.LightlyFried;
                            }

                            Dirty(entity, deepFriedComp);
                            _audio.PlayPvs(deepFryerComp.Buzzer, uid, AudioParams.Default.WithVolume(-5));
                        }
                    }
                }
            }
        }
    }
}
