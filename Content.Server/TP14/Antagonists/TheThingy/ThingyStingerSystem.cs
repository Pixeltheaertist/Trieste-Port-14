using Content.Server.Ninja.Events;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Thingy.Components;
using Content.Shared.Actions;

namespace Content.Server.Thingy.Systems
{

    // Summary
    // This system allows players that have been selected as a Thingy antagonist to be able to release and withdrawl their incapacitating bone stinger.
    // It is added as an action on init, allowing them to release or conceal it on command. The actual stinger incapacitating system uses DrifterToxin and is in knife.yml
    // Summary
    
    public sealed class ThingyStingerSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReleaseStingerComponent, CreateStingerEvent>(OnReleaseStinger);
        SubscribeLocalEvent<ReleaseStingerComponent, PullBackStingerEvent>(OnPullBackStinger);
    }

    private void OnReleaseStinger(Entity<ReleaseStingerComponent> ent, ref CreateStingerEvent args)
    {
        var (uid, comp) = ent;
        var user = args.User;

        if (_hands.GetHandCount(user) >= _hands.MaxHands)
        {
            return;
        }

        var ev = new CreateItemAttemptEvent(user);
        RaiseLocalEvent(uid, ref ev);
        if (ev.Cancelled)
            return;

        var stinger = Spawn(comp.SpawnedPrototype, Transform(user).Coordinates);

        if (!_hands.TryPickupAnyHand(user, stinger))
        {
            // Hands are full
            return;
        }

        comp.SpawnedStingerEntity = stinger;
        _actionsSystem.RemoveAction(user, ref stinger.ActionEntity, stinger.Action);
        _actionsSystem.AddAction(user, ref stinger.ActionEntity, stinger.ActionWithdrawl);
    }

    private void OnPullBackStinger(Entity<ReleaseStingerComponent> ent, ref PullBackStingerEvent args)
    {
        var (uid, comp) = ent;
        var user = args.User;

        if (comp.SpawnedStingerEntity == null)
            return;

        var stinger = comp.SpawnedStingerEntity.Value;

        QueueDel(stinger);

        comp.SpawnedStingerEntity = null;
        _actionsSystem.AddAction(user, ref stinger.ActionEntity, stinger.Action);
        _actionsSystem.RemoveAction(user, ref stinger.ActionEntity, stinger.ActionWithdrawl);
    }
}

}
