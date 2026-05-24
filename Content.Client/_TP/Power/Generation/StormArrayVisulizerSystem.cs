using Content.Shared._TP.Power.Generation;
using Robust.Client.GameObjects;

namespace Content.Client._TP.Power.Generation;

public sealed class StormArrayVisualizerSystem : VisualizerSystem<StormArrayComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, StormArrayComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<bool>(uid, StormArrayVisuals.Idle, out var idle, args.Component))
            idle = true;

        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), StormArrayVisuals.Idle, out var idleLayer, false))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), idleLayer, idle);


        if (!AppearanceSystem.TryGetData<bool>(uid, StormArrayVisuals.Active, out var active, args.Component))
            active = false;

        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), StormArrayVisuals.Active, out var activeLayer, false))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), activeLayer, active);
    }
}
