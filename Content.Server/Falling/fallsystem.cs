using System.Linq;
using Content.Shared.Ghost;
using Content.Server.Falling;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.Stunnable;
using Content.Shared.Damage.Components;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Content.Server.Popups;
using Content.Shared._TP;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.GameObjects;
using Content.Shared.Gravity;
using Robust.Server.GameObjects;
using Content.Shared.Shuttles.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Revenant.Components;
//Summary
// This system is the core piece of Trieste Port's falling system. Our "smoke and mirrors", if you will.
// It checks on both ParentChange and an Update loop for proper timing and confirmation. Doing this, it checks the parent of entities with the "FallSystemComponent".
// If said parent has the "TriesteAirSpaceComponent", then that means the entity is currently floating in Trieste's "airspace"
// After, it checks if said entity has the "JumpingComponent". This component marks when an entity is jumping. If an entity is currently in the air, it does not make them fall.
// If an entity is in the air, and is not jumping, it forces them to fall by teleporting them in a wide range around a marker in the "falling zone". 
// Finally, it applies knockdown and 80 blunt damage, as well as a popup informing of the fall. Anything with FallSystemComponent will be affected by the gravity, unless it is a ghost, flying mob, AI, or revenant.
// This means that one can hypothetically position a ship near Trieste, jump off of it onto the platform, and back, without falling.
//Summary
namespace Content.Server.Falling
{
    public sealed class FallSystem : EntitySystem
    {
        [Dependency] private readonly SharedStunSystem _stun = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

        private const int MaxRandomTeleportAttempts = 20; // The # of times it's going to try to find a valid spot to randomly teleport an object

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FallSystemComponent, EntParentChangedMessage>(OnEntParentChanged);
        }

        public override void Update(float frameTime) // Update loop my beloathed
        {
            foreach (var entity in EntityQuery<FallSystemComponent>()) // Find all entities with the FallSystem component
            {
                var EntityParent = Transform(entity.Owner).ParentUid; // Get the entity's parent UID

                // If our entity's parent has the TriesteAirspaceComponent, we know that it's currently in the air
                if (HasComp<TriesteAirspaceComponent>(EntityParent))
                {
                        // Confirm if the entity is actively jumping. If so, skip over them.
                        if (TryComp<JumpingComponent>(entity.Owner, out var jumping))
                        {
                            if (jumping.IsJumping)
                            {
                             continue;
                            }
                             else
                            {
                                // YEET THINE ENTITY
                             HandleFall(entity.Owner, entity);
                             continue;
                            }
                        }
                        // YEET THINE ENTITY PART TWO
                        HandleFall(entity.Owner, fallSystemComponent);
                        continue;
                }
                else
                {
                    // Otherwise, skip over the entity and continue down the loop
                    continue;
                }
            }
        }

        private void OnEntParentChanged(EntityUid owner, FallSystemComponent component, EntParentChangedMessage args) // called when the entity changes parents
        {
            if (args.OldParent == null || args.Transform.GridUid != null ||
                TerminatingOrDeleted(
                    owner)) // If you came from space or are switching to another valid grid, nothing happens.
            {
                return;
            }

            var OwnerParent = Transform(owner).ParentUid;
            // If yer not in Trieste airspace, FUCK OFF
            if (!HasComp<TriesteAirspaceComponent>(OwnerParent))
            {
                return;
            }

            // For observers
            if (HasComp<GhostComponent>(owner))
            {
                return;
            }

            // For things like AI
            if (HasComp<NoFTLComponent>(owner))
            {
                return;
            }

            // Flying enemies and mobs
            if (HasComp<CanMoveInAirComponent>(owner))
            {
                return;
            }

            // Revenants aren't ghosts for some reason
            if (HasComp<RevenantComponent>(owner))
            {
                return;
            }

            // Jumping is handled more specifically in the update loop
            if (HasComp<JumpingComponent>(owner))
            {
                return;
            }

            // YYYYEEEEEEEEEEEEETTT!!
            HandleFall(owner, component);
        }

        // Finds your destination
        private void HandleFall(EntityUid owner, FallSystemComponent component)
        {
            // Hey, look! It's your destination
            var destination = EntityManager.EntityQuery<FallingDestinationComponent>().FirstOrDefault();
            if (destination != null)
            {
                // Teleport to the first destination's coordinates
                // Teleports you to the destination
                Transform(owner).Coordinates = Transform(destination.Owner).Coordinates;
            }
            else
            {
                // If there's no destination, something broke
                Log.Error($"No valid falling sites available!");
                return;
            }

            // Stuns the fall-ee for five seconds
            var stunTime = TimeSpan.FromSeconds(5);
            _stun.TryKnockdown(owner, stunTime, refresh: true);
            _stun.TryStun(owner, stunTime, refresh: true);

            // Defines the damage being dealt, ouch!
            var damage = new DamageSpecifier
            {
                DamageDict = { ["Blunt"] = 80f }
            };
            _damageable.TryChangeDamage(owner, damage, origin: owner);

            // Causes a popup
            _popup.PopupEntity(Loc.GetString("fell-to-seafloor"), owner, PopupType.LargeCaution);

            // Randomly teleports you in a radius around the landing zone (this is pretty much damn near instantaneous in paralell with the rest of the code, so it's fine to happen after the teleport)
            TeleportRandomly(owner, component);
        }

        private void TeleportRandomly(EntityUid owner, FallSystemComponent component)
        {
            var coords = Transform(owner).Coordinates;
            var newCoords = coords; // Start with the current coordinates

            for (var i = 0; i < MaxRandomTeleportAttempts; i++) // Does this for a set amount of time before giving up, or else it'll get stuck and constantly try to find a site
            {
                // Generate a random offset based on a defined radius
                var offset = _random.NextVector2(component.MaxRandomRadius);
                newCoords = coords.Offset(offset);

                // Check if the new coordinates are free of static entities
                if (!_lookup.GetEntitiesIntersecting(newCoords.ToMap(EntityManager, _transformSystem), LookupFlags.Static).Any())
                {
                    break; // Found a valid location
                }
            }

            // Set the new coordinates to teleport the entity
            // GET TELEPORTED, IDIOT
            Transform(owner).Coordinates = newCoords;
        }
    }
}
