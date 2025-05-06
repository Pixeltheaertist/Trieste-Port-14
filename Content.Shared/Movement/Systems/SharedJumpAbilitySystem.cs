using Content.Shared.Throwing;
using Content.Shared.Movement.Components;
using Robust.Shared.Audio.Systems;
using Content.Shared.Gravity;
using Content.Shared._TP;
using Robust.Shared.Timing;

namespace Content.Shared.Movement.Systems;

public sealed partial class SharedJumpAbilitySystem : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JumpAbilityComponent, GravityJumpEvent>(OnGravityJump);
    }

    public override void Update(float frameTime)
    {

        foreach (var entity in EntityQuery<JumpingComponent>())
        {

          if (!entity.IsJumping)
          {
              return;
          }
          else if (_timing.CurTime >= entity.LastJumped + TimeSpan.FromSeconds(1))
          {
              entity.IsJumping = false;
          }
        }
    }

    private void OnGravityJump(Entity<JumpAbilityComponent> entity, ref GravityJumpEvent args)
    {
        if (_gravity.IsWeightless(args.Performer))
            return;

        EnsureComp<JumpingComponent>(args.Performer, out var jump);
        jump.IsJumping = true;
        jump.LastJumped = _timing.CurTime;
        var xform = Transform(args.Performer);
        var throwing = xform.LocalRotation.ToWorldVec() * entity.Comp.JumpDistance;
        var direction = xform.Coordinates.Offset(throwing); // to make the character jump in the direction he's looking

        _throwing.TryThrow(args.Performer, direction, entity.Comp.JumpThrowSpeed);

        _audio.PlayPredicted(entity.Comp.JumpSound, args.Performer, args.Performer);
        args.Handled = true;
    }
}
