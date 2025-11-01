using System.Linq;
using Content.Server.Popups;
using Content.Shared.Climbing.Components;
using Content.Shared.Climbing.Systems;
using Content.Shared.Damage;
using Content.Shared.Ghost;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Revenant.Components;
using Content.Shared.Salvage.Fulton;
using Content.Shared.Shuttles.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._TP.Falling.Systems
{
    public sealed class FallSystem : EntitySystem
    {
        [Dependency] private readonly SharedStunSystem _stun = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly ClimbSystem _climb = default!;

        private const int MaxRandomTeleportAttempts = 20; // The # of times it's going to try to find a valid spot to randomly teleport an object

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<Components.FallSystemComponent, EntParentChangedMessage>(OnEntParentChanged);
        }

        public override void Update(float frameTime)
        {
            // First we start with an entity enumerator for the JumpingComponent (aka players)
            // If it passes, start a while loop and get the UID/component.
            // If the player is on a grid, run 'continue' to skip falling.
            var jumpingQuery = EntityQueryEnumerator<JumpingComponent>();
            while (jumpingQuery.MoveNext(out var uid, out var jumpComp))
            {
                var transform = _transformSystem.GetGrid(uid);
                if (transform != null)
                {
                    continue;
                }

                // Now check if the entity WAS jumping.
                // If it was, and it's not jumping anymore, it will fall.
                // At the end we set wasJumping to IsJumping.
                var entityParent = _transformSystem.GetParentUid(uid);
                if (HasComp<TriesteComponent>(entityParent) &&
                    jumpComp is { IsJumping: false, WasJumping: true })
                {
                    if (TryComp<Components.FallSystemComponent>(uid, out var fallSystemComponent))
                        HandleFall(uid, fallSystemComponent);
                }

                if (transform != null)
                {
                    jumpComp.WasJumping = jumpComp.IsJumping;
                }
            }

            // This part catches if the player has climbed over the railing
            // and will force them to stop (and fall)!
            var fallQuery = EntityQueryEnumerator<Components.FallSystemComponent>();
            while (fallQuery.MoveNext(out var uid, out var fallComp))
            {
                // Skip if already handled by jumping logic above
                if (HasComp<JumpingComponent>(uid))
                    continue;

                var transform = _transformSystem.GetGrid(uid);
                if (transform != null)
                    continue; // Still on a grid, don't fall

                var entityParent = _transformSystem.GetParentUid(uid);
                if (HasComp<TriesteComponent>(entityParent))
                {
                    // Check if they should be exempt from falling
                    if (ExemptFromFalling(uid))
                        continue;

                    // Now check if they've been knocked down (aka slipped)
                    if (TryComp<KnockedDownComponent>(uid, out _))
                        HandleFall(uid, fallComp);

                    // Force stop climbing if they're in airspace
                    if (TryComp<ClimbingComponent>(uid, out var climbComp) && climbComp.IsClimbing)
                        _climb.StopClimb(uid, climbComp);

                    HandleFall(uid, fallComp);
                }
            }
        }


        private void OnEntParentChanged(EntityUid owner, Components.FallSystemComponent component, EntParentChangedMessage args) // called when the entity changes parents
        {
            // A check that should fix the round-start crash/restart. - Cookie (FatherCheese)
            // If the entity is not initialized, or we're below 10 seconds, return.
            if (MetaData(owner).EntityLifeStage < EntityLifeStage.MapInitialized)
            {
                return;
            }

            // A check to see if the player jumped from one grid to
            // another, and if so, return, so they don't fall.
            if (args.OldParent == null || args.Transform.GridUid != null ||
                TerminatingOrDeleted(
                    owner))
            {
                return;
            }

            if (ExemptFromFalling(owner))
                return;

            var ownerParent = Transform(owner).ParentUid;
            if (!HasComp<TriesteAirspaceComponent>(ownerParent))
            {
                return;
            }

            // Force stop climbing when entering airspace via parent change
            if (TryComp<ClimbingComponent>(owner, out var climbComp) && climbComp.IsClimbing)
            {
                _climb.StopClimb(owner, climbComp);
            }

            HandleFall(owner, component);
        }

        /// <summary>
        ///     A method of different components that are exempt from falling.
        /// </summary>
        /// <param name="owner">Entity UID</param>
        /// <returns>Returns true if the comp exists, otherwise returns false</returns>
        private bool ExemptFromFalling(EntityUid owner)
        {
            if (HasComp<FultonedComponent>(owner))
            {
                return true;
            }

            if (HasComp<GhostComponent>(owner))
            {
                return true;
            }

            if (HasComp<NoFTLComponent>(owner))
            {
                return true;
            }

            if (HasComp<CanMoveInAirComponent>(owner))
            {
                return true;
            }

            if (HasComp<RevenantComponent>(owner))
            {
                return true;
            }

            if (HasComp<JumpingComponent>(owner))
            {
                return true;
            }

            return false;
        }

        private void HandleFall(EntityUid owner, Components.FallSystemComponent component)
        {
            var destination = EntityManager.EntityQuery<Components.FallingDestinationComponent>().FirstOrDefault();
            if (destination != null)
            {
                // Teleport to the first destination's coordinates
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
            _stun.TryAddStunDuration(owner, stunTime);

            // Defines the damage being dealt
            var damage = new DamageSpecifier
            {
                DamageDict = { ["Blunt"] = 80f }
            };
            _damageable.TryChangeDamage(owner, damage, origin: owner);

            // Causes a popup
            _popup.PopupEntity(Loc.GetString("fell-to-seafloor"), owner, PopupType.LargeCaution);

            // Randomly teleports you in a radius around the landing zone
            TeleportRandomly(owner, component);
        }

        private void TeleportRandomly(EntityUid owner, Components.FallSystemComponent component)
        {
            var coords = Transform(owner).Coordinates;
            var newCoords = coords; // Start with the current coordinates

            for (var i = 0; i < MaxRandomTeleportAttempts; i++)
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
            _transformSystem.SetCoordinates(owner, newCoords);
        }
    }
}
