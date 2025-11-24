using System.Linq;
using System.Numerics;
using Content.Client._Moffstation.GameObjects;
using Content.Shared._Moffstation.Cards.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Moffstation.Cards;

public sealed partial class CardHandVisualizerSystem : ManagedLayerVisualizerSystem<CardHandComponent>
{
    protected override ref HashSet<string> SpriteLayersAdded(CardHandComponent component) =>
        ref component.SpriteLayersAdded;

    protected override void AddLayersOnAppearanceChange(
        CardHandComponent component,
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

        var startingAngle = -(component.Angle / 2);
        var intervalAngle = visibleCards.Length != 1 ? component.Angle / (visibleCards.Length - 1) : 0;
        var startingXOffset = -(component.XOffset / 2);
        var intervalOffset = visibleCards.Length != 1 ? component.XOffset / (visibleCards.Length - 1) : 0;
        var layerScale = new Vector2(component.Scale, component.Scale);
        foreach (var (cardIndex, cardEnt) in GetEntityArray(visibleCards).Index())
        {
            if (!TryComp<CardComponent>(cardEnt, out var cardComp))
                continue;

            foreach (var (layerIndex, layerData) in cardComp.CurrentSprite.Index())
            {
                var layer = layerFactory($"{cardIndex}-{layerIndex}", layerData);

                var angle = startingAngle + cardIndex * intervalAngle;
                var x = startingXOffset + cardIndex * intervalOffset;
                var y = -(x * x) + 0.10f;

                SpriteSystem.LayerSetRotation(layer, Angle.FromDegrees(-angle));
                SpriteSystem.LayerSetOffset(layer, new Vector2(x, y));
                SpriteSystem.LayerSetScale(layer, layerScale);
            }
        }
    }
}
