using Content.Server.Ninja.Events;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Thingy.Components;
using Content.Shared.Actions;

namespace Content.Server.Thingy.Systems
{
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
            // Hands are full, stinger cannot be held, don't drop it
            // You could notify the player here if needed
            return;
        }

        comp.SpawnedStingerEntity = stinger;
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
    }
}

}
