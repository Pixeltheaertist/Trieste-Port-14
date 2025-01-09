using Content.Server.Ninja.Events;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Thingy.Components;  // Your custom component
using Content.Shared.Actions;  // For the custom action system

namespace Content.Server.Thingy.Systems
{
    public sealed class ThingyStingerSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe to the custom stinger creation event
        SubscribeLocalEvent<ReleaseStingerComponent, CreateStingerEvent>(OnReleaseStinger);

        // Subscribe to the pull-back stinger event
        SubscribeLocalEvent<ReleaseStingerComponent, PullBackStingerEvent>(OnPullBackStinger);
    }

    // Event handler for releasing the stinger and spawning it
    private void OnReleaseStinger(Entity<ReleaseStingerComponent> ent, ref CreateStingerEvent args)
    {
        var (uid, comp) = ent;
        var user = args.User;

        // If hands are full, do nothing
        if (_hands.GetHandCount(user) >= _hands.MaxHands)
        {
            // Optionally, you could send a message to the user here
            return;
        }

        // Raise the item creation attempt event for the stinger
        var ev = new CreateItemAttemptEvent(user);
        RaiseLocalEvent(uid, ref ev);
        if (ev.Cancelled)
            return;

        // Spawn the stinger at the user's location
        var stinger = Spawn(comp.SpawnedPrototype, Transform(user).Coordinates);

        // Try to place the stinger in the user's hand
        if (!_hands.TryPickupAnyHand(user, stinger))
        {
            // Hands are full, stinger cannot be held, don't drop it
            // You could notify the player here if needed
            return;
        }

        // Store the stinger entity on the component to allow pulling it back
        comp.SpawnedStingerEntity = stinger;
    }

    // Event handler for pulling back the stinger
    private void OnPullBackStinger(Entity<ReleaseStingerComponent> ent, ref PullBackStingerEvent args)
    {
        var (uid, comp) = ent;
        var user = args.User;

        // Check if the user has released a stinger to pull back
        if (comp.SpawnedStingerEntity == null)
            return;

        var stinger = comp.SpawnedStingerEntity.Value;

        // Destroy or remove the stinger
        QueueDel(stinger);

        // Optionally, notify the user that the stinger has been retracted
        comp.SpawnedStingerEntity = null;
    }
}

}
