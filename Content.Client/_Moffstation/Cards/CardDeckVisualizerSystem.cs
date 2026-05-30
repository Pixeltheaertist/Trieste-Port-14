using System.Linq;
using System.Numerics;
using Content.Client._Moffstation.GameObjects;
using Content.Shared._Moffstation.Cards.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Moffstation.Cards;

public sealed partial class CardDeckVisualizerSystem : ManagedLayerVisualizerSystem<CardDeckComponent>
{
    protected override ref HashSet<string> SpriteLayersAdded(CardDeckComponent component) =>
        ref component.SpriteLayersAdded;

    protected override void AddLayersOnAppearanceChange(
        CardDeckComponent component,
        Entity<SpriteComponent?> sprite,
        AppearanceComponent appearance,
        Func<string, PrototypeLayerData, SpriteComponent.Layer> layerFactory
    )
    {
        if (!AppearanceSystem.TryGetData<NetEntity[]>(
                sprite,
                CardStackVisuals.Cards,
                out var visibleCards,
                appearance
            ))
            return;

        var layerRotation = Angle.FromDegrees(90);
        var layerScale = new Vector2(component.Scale, component.Scale);
        foreach (var (cardIndex, cardEnt) in GetEntityArray(visibleCards).Index())
        {
            if (!TryComp<CardComponent>(cardEnt, out var cardComp))
                continue;

            foreach (var (cardLayerIndex, cardLayerData) in cardComp.CurrentSprite.Index())
            {
                var layer = layerFactory($"{cardIndex}-{cardLayerIndex}", cardLayerData);
                SpriteSystem.LayerSetRotation(layer, layerRotation);
                SpriteSystem.LayerSetScale(layer, layerScale);
                SpriteSystem.LayerSetOffset(layer, new Vector2(0, component.YOffset * cardIndex));
            }
        }
    }
}
