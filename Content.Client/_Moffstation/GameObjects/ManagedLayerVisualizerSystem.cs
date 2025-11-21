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
            if (!SpriteSystem.LayerExists(sprite, layerAdded))
            {
                // TODO Workaround for https://github.com/space-wizards/RobustToolbox/pull/6305
                if (SpriteSystem.LayerMapGet(sprite, layerAdded) != 0)
                {
                    DebugTools.Assert($"Failed to retrieve added layer: {layerAdded}");
                    continue;
                }

                Log.Debug($"Suppressing possibly incorrect layer lookup failure: \"{layerAdded}\"!");
            }

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
                DebugTools.Assert(SpriteSystem.TryGetLayer(sprite, newLayerIndex, out var layer, logMissing: true));
                addedLayers.Add(newLayerKey);
                return layer;
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
