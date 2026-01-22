using Content.Shared._TP.Kitchen.Components;
using Robust.Client.GameObjects;

namespace Content.Client._TP14.Kitchen;

public sealed class CoffeeMakerVisualizerSystem : VisualizerSystem<SharedCoffeeMakerComponent>
{
        protected override void OnAppearanceChange(EntityUid uid,
        SharedCoffeeMakerComponent component,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        AppearanceSystem.TryGetData<bool>(uid, CoffeeMakerVisuals.Pitcher, out var pitcher, args.Component);
        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), CoffeeMakerVisuals.Pitcher, out var pitcherLayer, false))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), pitcherLayer, pitcher);

        AppearanceSystem.TryGetData<bool>(uid, CoffeeMakerVisuals.Basket, out var basket, args.Component);
        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), CoffeeMakerVisuals.Basket, out var basketLayer, false))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), basketLayer, basket);
    }
}
