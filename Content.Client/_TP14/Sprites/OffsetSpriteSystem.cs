using Content.Shared._TP.Sprite;
using Robust.Client.GameObjects;

namespace Content.Client._TP14.Sprites;

/// <summary>
///     Shared-side logic for the OffsetSpriteComponent. This should tie into the sprite component.
///     Created by Cookie (FatherCheese) for Trieste Port 14.
/// </summary>
public sealed class OffsetSpriteSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OffsetSpriteComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(Entity<OffsetSpriteComponent> ent, ref ComponentStartup args)
    {
        var offsetComp = ent.Comp;

        if (!TryComp<SpriteComponent>(ent.Owner, out _))
        {
            Log.Warning($"Entity {ent.Owner} has an OffsetSpriteComponent but no SpriteComponent.");
            return;
        }

        var rot = Transform(ent).LocalRotation;
        var dir = rot.GetCardinalDir();

        if (!offsetComp.DirectionOffsets.TryGetValue(dir, out var offset))
        {
            Log.Warning($"No offset defined for direction {dir}");
            return;
        }

        _sprite.SetOffset(ent.Owner, offset);
    }
}
