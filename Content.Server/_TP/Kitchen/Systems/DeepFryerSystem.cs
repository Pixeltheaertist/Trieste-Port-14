using Content.Server.Administration.Logs;
using Content.Server.Hands.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._TP.Kitchen;
using Content.Shared._TP.Kitchen.Events;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._TP.Kitchen.Systems;


public sealed class DeepFryerSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
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

    /// <summary>
    ///     Prevents memory leaks.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnShutdown(Entity<SharedDeepFryerComponent> ent, ref ComponentShutdown args)
    {
        if (_fryerSounds.TryGetValue(ent, out var soundEntity) && soundEntity != null)
            _audio.Stop(soundEntity.Value);

        _fryerSounds.Remove(ent);

        if (_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
        {
            foreach (var entity in container.ContainedEntities)
            {
                _cookingStartTimes.Remove(entity);
            }
        }
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

        if (TryComp<DamageableComponent>(args.Used, out _))
        {
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User):Player} put {ToPrettyString(args.Used)} into {ToPrettyString(ent):DeepFryer}");
        }

        var usedMeta = MetaData(args.Used);
        if (usedMeta.EntityName.StartsWith("burnt"))
        {
            _popup.PopupEntity(Loc.GetString("Deep-Fryer-Message-Burnt-Item", ("item", args.Used)), ent, args.User);
            args.Handled = true;
            return;
        }


        if (!_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.SolutionContainerId, out _, out var solName))
            return;

        if (solName.Volume <= 25)
        {
            _popup.PopupEntity(Loc.GetString("Deep-Fryer-Message-Low-Oil", ("fryer", ent.Owner)), ent, args.User);
            args.Handled = true;
            return;
        }

        if (!ent.Comp.IsEnabled)
        {
            _popup.PopupEntity(Loc.GetString("Deep-Fryer-Message-Disabled", ("fryer", ent.Owner)), ent, args.User);
            args.Handled = true;
            return;
        }

        if (TryComp<ItemComponent>(args.Used, out var itemComp))
        {
            if (itemComp.Size == "Small" || itemComp.Size == "Tiny" || itemComp.Size != "Large")
                return;

            _popup.PopupEntity(Loc.GetString("Deep-Fryer-Message-Large-Item", ("item", args.Used)), ent, args.User);
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
            _popup.PopupEntity(Loc.GetString("Deep-Fryer-No-Power", ("fryer", deepFryerEnt.Owner)), deepFryerEnt, args.User);
            return;
        }

        // Now we check if the deep fryer is broken. If so, popup a message and return.
        if (deepFryerComp.IsBroken)
        {
            _popup.PopupEntity(Loc.GetString("Deep-Fryer-Message-Broken", ("fryer", deepFryerEnt.Owner)), deepFryerEnt, args.User);
            return;
        }

        // NOW we check if the deep fryer has enough oil. This is done via Olive Oil for now.
        // This is also done with two checks - a total volume, and specifically olive oil.
        // If either are false, popup and return.
        if (!_solutionContainer.TryGetSolution(deepFryerEnt.Owner, deepFryerComp.SolutionContainerId, out _, out var solName))
            return;

        if (solName.Volume <= 25)
        {
            _popup.PopupEntity(Loc.GetString("Deep-Fryer-Message-Low-Oil", ("fryer", deepFryerEnt.Owner)), deepFryerEnt, args.User);
            return;
        }

        var cookingOilAmnt = solName.GetTotalPrototypeQuantity("OilOlive");
        if (cookingOilAmnt <= 25)
        {
            _popup.PopupEntity(Loc.GetString("Deep-Fryer-Message-Low-Oil", ("fryer", deepFryerEnt.Owner)), deepFryerEnt, args.User);
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
                _popup.PopupEntity(Loc.GetString("Deep-Fryer-Message-Cooking-Item", ("item", entity)), deepFryerEnt, args.User);
                continue;
            }

            _popup.PopupEntity(Loc.GetString("Deep-Fryer-Message-Grabbed-Item",
                    ("item", entity),
                    ("fryer", deepFryerEnt.Owner)),
                deepFryerEnt,
                args.User);

            _popup.PopupEntity(Loc.GetString("Deep-Fryer-Message-Grabbed-Item-Others",
                    ("player", args.User),
                    ("item", entity),
                    ("fryer", deepFryerEnt.Owner)),
                deepFryerEnt,
                Filter.PvsExcept(args.User),
                true);

            _container.Remove(entity, container);
            _hands.PickupOrDrop(args.User, entity);
            _cookingStartTimes.Remove(entity);
        }

        // Otherwise, if the container has NO items, we toggle the deep fryer with two popups.
        if (container.ContainedEntities.Count == 0)
        {
            _popup.PopupEntity(deepFryerComp.IsEnabled
                ? Loc.GetString("Deep-Fryer-Message-Toggle-Off", ("fryer", deepFryerEnt.Owner))
                : Loc.GetString("Deep-Fryer-Message-Toggle-On", ("fryer", deepFryerEnt.Owner)),
            deepFryerEnt,
            args.User);

            _popup.PopupEntity(deepFryerComp.IsEnabled
                ? Loc.GetString("Deep-Fryer-Message-Toggle-Off-Others", ("player", args.User), ("fryer", deepFryerEnt.Owner))
                : Loc.GetString("Deep-Fryer-Message-Toggle-On-Others", ("player", args.User), ("fryer", deepFryerEnt.Owner)),
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
                    FryFoodEntity(entity, uid, deepFryerComp, recipe);
                }
                else
                {
                    FryNonFoodEntity(entity, uid, deepFryerComp);
                }
            }
        }
    }

    /// <summary>
    ///     Cooks the supplied entity as long as it has a recipe prototype.
    /// </summary>
    /// <param name="friedEntUid">The inserted entity uid</param>
    /// <param name="fryerEntUid">The fryer entity uid</param>
    /// <param name="deepFryerComp">The deep-fryer component</param>
    /// <param name="recipe">The recipe prototype</param>
    private void FryFoodEntity(EntityUid friedEntUid,
        EntityUid fryerEntUid,
        SharedDeepFryerComponent deepFryerComp,
        DeepFryingRecipePrototype recipe)
    {
        // First, we start by getting the container and the cooking time.
        // If the container doesn't exist, we return early.
        // If the cooking time doesn't exist, we set it to the current time.'
        if (!_container.TryGetContainer(fryerEntUid, deepFryerComp.ContainerId, out var container))
            return;

        if (!_cookingStartTimes.TryGetValue(friedEntUid, out var startTime))
        {
            _cookingStartTimes[friedEntUid] = _timing.CurTime;
        }

        // Now we check if the elapsed time is greater than the cook time.
        // If it is, we obviously cook it. Otherwise, return early.
        var elapsed = _timing.CurTime - startTime;
        if (elapsed.TotalSeconds < recipe.CookTime)
            return;

        // Now we set the container to "remove" the fried entity.
        // Once removed, we spawn the recipe result and insert it into the container. Seamless!
        _container.Remove(friedEntUid, container);
        QueueDel(friedEntUid);

        var recipeResult = Spawn(recipe.Result, Transform(fryerEntUid).Coordinates);
        _container.Insert(recipeResult, container);

        // Now we remove the cooking time and play the buzzer sound.
        _cookingStartTimes.Remove(friedEntUid);
        _audio.PlayPvs(deepFryerComp.Buzzer, fryerEntUid, AudioParams.Default.WithVolume(-5));
    }

    /// <summary>
    ///     Cooks non-recipe item entities, applying a sprite overlay and damage to damageables.
    /// </summary>
    /// <param name="friedEntUid">The inserted entity uid</param>
    /// <param name="fryerEntUid">The deep-fryer entity uid</param>
    /// <param name="deepFryerComp">The deep-fryer component</param>
    private void FryNonFoodEntity(EntityUid friedEntUid, EntityUid fryerEntUid, SharedDeepFryerComponent deepFryerComp)
    {
        // We start by getting the container. If it doesn't have one (which it shouldn't), return.
        if (!_container.TryGetContainer(fryerEntUid, deepFryerComp.ContainerId, out var container))
            return;

        // Now we get the item's metadata. This is for later.
        var itemMeta = MetaData(friedEntUid);

        // We set the cooking start time if the entity doesn't have one yet.
        if (!_cookingStartTimes.ContainsKey(friedEntUid))
            _cookingStartTimes[friedEntUid] = _timing.CurTime;

        //  Now we check for a damageable component. If it has one, we apply 1.5 heat damage.
        //  This is just so living/hurtable entities can't survive the deep fryer.
        if (TryComp<DamageableComponent>(friedEntUid, out _))
        {
            var damage = new DamageSpecifier
            {
                DamageDict = { ["Heat"] = 0.33f },
            };
            _damageable.TryChangeDamage(friedEntUid, damage, origin: fryerEntUid);
        }

        // Now check for the start and elapsed times.
        // If the elapsed time is greater than the cook time, we obviously cook it. Otherwise, return.
        if (!_cookingStartTimes.TryGetValue(friedEntUid, out var startTime))
        {
            _cookingStartTimes[friedEntUid] = _timing.CurTime;
        }

        var elapsed = _timing.CurTime - startTime;
        if (elapsed.TotalSeconds < deepFryerComp.CookTimePerLevel)
            return;

        // Now we ensure the entity inside has a "fried" component.
        // Then we remove the cooking time and set fry level and name based on the PREVIOUS fry level.
        // This defaults as "None", so "Lightly-Fried" is the first one.
        EnsureComp<SharedDeepFriedComponent>(friedEntUid, out var deepFriedComp);

        _cookingStartTimes.Remove(friedEntUid);

        if (itemMeta.EntityName.StartsWith("lightly-fried"))
        {
            _metaData.SetEntityName(friedEntUid, itemMeta.EntityName.Replace("lightly-fried", "fried"));
            deepFriedComp.CurrentFriedLevel = SharedDeepFriedComponent.FriedLevel.Fried;
        }
        else if (itemMeta.EntityName.StartsWith("fried"))
        {
            _metaData.SetEntityName(friedEntUid, itemMeta.EntityName.Replace("fried", "burnt"));
            deepFriedComp.CurrentFriedLevel = SharedDeepFriedComponent.FriedLevel.Burnt;

            // "Burnt" gets a special function, in that it drops out and can't be re-inserted.
            _container.InsertOrDrop(friedEntUid, container);
        }
        else
        {
            _metaData.SetEntityName(friedEntUid, itemMeta.EntityName.Insert(0, "lightly-fried "));
            deepFriedComp.CurrentFriedLevel = SharedDeepFriedComponent.FriedLevel.LightlyFried;
        }

        // Once the entity is fried, we dirty the entity and raise an event for sprite change.
        // We also play a buzzer sound.
        Dirty(friedEntUid, deepFriedComp);
        RaiseLocalEvent(friedEntUid, new DeepFriedLevelChangedEvent());
        _cookingStartTimes.Remove(friedEntUid);
        _audio.PlayPvs(deepFryerComp.Buzzer, fryerEntUid, AudioParams.Default.WithVolume(-5));
    }
}
