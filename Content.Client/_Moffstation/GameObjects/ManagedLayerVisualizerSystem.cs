using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Moffstation.GameObjects;

public abstract class ManagedLayerVisualizerSystem<TComp> : VisualizerSystem<TComp> where TComp : Component
{
    private static readonly string LayerPrefix = $"{typeof(TComp).Name}-ManagedLayer-";

    protected override void OnAppearanceChange(EntityUid uid, TComp component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        ref var layersAdded = ref SpriteLayersAdded(component);

        // Obliterate existing layers
        var sprite = new Entity<SpriteComponent?>(uid, args.Sprite);
        foreach (var layerAdded in layersAdded)
        {
            SpriteSystem.RemoveLayer(sprite, layerAdded);
        }

        layersAdded.Clear();

        var addedLayers = new HashSet<string>();
        AddLayersOnAppearanceChange(
            component,
            sprite,
            args.Component,
            (partialLayerName, layerData) =>
            {
                var newLayerKey = LayerPrefix + partialLayerName;
                var newLayerIndex = SpriteSystem.AddLayer(sprite, layerData, null);
                SpriteSystem.LayerMapAdd(sprite, newLayerKey, newLayerIndex);
                addedLayers.Add(newLayerKey);
                // ReSharper disable once RedundantAssignment // It's used by a debug assert, you piece.
                var gotLayer = SpriteSystem.TryGetLayer(sprite, newLayerIndex, out var layer, logMissing: true);
                DebugTools.Assert(gotLayer);
                return layer ?? SpriteSystem.AddBlankLayer((sprite, sprite.Comp!));
            }
        );
        layersAdded.UnionWith(addedLayers);
    }

    protected abstract ref HashSet<string> SpriteLayersAdded(TComp component);

    protected abstract void AddLayersOnAppearanceChange(
        TComp component,
        Entity<SpriteComponent?> sprite,
        AppearanceComponent appearance,
        Func<string, PrototypeLayerData, SpriteComponent.Layer> layerFactory
    );
}
