using System.Linq;
using Content.Client._Moffstation.GameObjects;
using Content.Shared._Moffstation.Cards.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Moffstation.Cards;

public sealed class CardVisualizerSystem : ManagedLayerVisualizerSystem<CardComponent>
{
    protected override ref HashSet<string> SpriteLayersAdded(CardComponent component) =>
        ref component.SpriteLayersAdded;

    protected override void AddLayersOnAppearanceChange(
        CardComponent component,
        Entity<SpriteComponent?> sprite,
        AppearanceComponent appearance,
        Func<string, PrototypeLayerData, SpriteComponent.Layer> layerFactory
    )
    {
        foreach (var (layerIndex, layerData) in component.CurrentSprite.Index())
        {
            layerFactory($"{layerIndex}", layerData);
        }
    }
}
