using System.Linq;
using Content.Server._TP.Ladder;
using Content.Server.Popups;
using Content.Shared.Climbing.Components;
using Content.Shared.Climbing.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Ghost;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Revenant.Components;
using Content.Shared.Salvage.Fulton;
using Content.Shared.Shuttles.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Stunnable;
using Robust.Shared.Map;

namespace Content.Server._TP.Falling.Systems;

public sealed class FallSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;

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

            if (transform != null && jumpComp.WasJumping != jumpComp.IsJumping)
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

    private void OnEntParentChanged(EntityUid owner, Components.FallSystemComponent component, EntParentChangedMessage args)
    {
        // A check that should fix the round-start crash/restart. - Cookie (FatherCheese)
        // If the entity is not initialized, or we're below 10 seconds, return.
        if (MetaData(owner).EntityLifeStage < EntityLifeStage.MapInitialized)
        {
            return;
        }

        // A check to see if the player jumped from one grid to
        // another, and if so, return so they don't fall.
        if (args.OldParent == null || args.Transform.GridUid != null ||
            TerminatingOrDeleted(owner))
        {
            return;
        }

        if (ExemptFromFalling(owner))
            return;

        var ownerParent = Transform(owner).ParentUid;
        if (!HasComp<TriesteComponent>(ownerParent))
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
        return HasComp<FultonedComponent>(owner)
               || HasComp<GhostComponent>(owner)
               || HasComp<NoFTLComponent>(owner)
               || HasComp<CanMoveInAirComponent>(owner)
               || HasComp<RevenantComponent>(owner)
               || HasComp<JumpingComponent>(owner)
               || HasComp<StationAiHeldComponent>(owner);
    }

    private void HandleFall(EntityUid owner, Components.FallSystemComponent component)
    {
        var destination = EntityManager.EntityQuery<Components.FallingDestinationComponent>().FirstOrDefault();
        if (destination == null)
        {
            // If there's no destination, something broke
            Log.Error($"No valid falling sites available!");
            return;
        }

        // Now we set variables for Wastewater and Trieste's ladders.
        // Essentially, we get both ladder's positions as a marker point and offset the player's fall position.
        // We have to do this due to Trieste using trade station, and being offset by like 700 in any given direction.
        var sourceLadder = EntityManager.EntityQuery<LadderComponent>()
        .FirstOrDefault(ladder => _transformSystem.GetMapCoordinates(ladder.Owner).MapId ==
                                  _transformSystem.GetMapCoordinates(owner).MapId);

        var destLadder = EntityManager.EntityQuery<LadderComponent>()
        .FirstOrDefault(ladder => _transformSystem.GetMapCoordinates(ladder.Owner).MapId ==
                                  _transformSystem.GetMapCoordinates(destination.Owner).MapId);

        if (sourceLadder == null || destLadder == null)
        {
            Log.Error($"Could not find ladders for fall calculation!");
            return;
        }

        // Now we get the coordinates of the owner and ladders.
        var ownerCoords = _transformSystem.GetMapCoordinates(owner);
        var sourceLadderCoords = _transformSystem.GetMapCoordinates(sourceLadder.Owner);
        var destLadderCoords = _transformSystem.GetMapCoordinates(destLadder.Owner);

        // THEN we calculate and apply the offset between the ladders and player's position.
        var ladderOffset = destLadderCoords.Position - sourceLadderCoords.Position;
        var newPosition = ownerCoords.Position + ladderOffset;
        var newCoords = new MapCoordinates(newPosition, destLadderCoords.MapId);
        _transformSystem.SetMapCoordinates(owner, newCoords);

        // Then we stun the fall-ee for five seconds, as well as apply EIGHTY blunt damage.
        // Finally, at the bottom we cause a pop-up saying something fell.
        var stunTime = TimeSpan.FromSeconds(5);
        _stun.TryKnockdown(owner, stunTime, refresh: true);
        _stun.TryAddStunDuration(owner, stunTime);

        var damage = new DamageSpecifier
        {
            DamageDict = { ["Blunt"] = 80f }
        };
        _damageable.TryChangeDamage(owner, damage, origin: owner);
        _popup.PopupEntity(Loc.GetString("fell-to-seafloor"), owner, PopupType.LargeCaution);
    }
}
