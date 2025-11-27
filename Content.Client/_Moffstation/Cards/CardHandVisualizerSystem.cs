using System.Linq;
using System.Numerics;
using Content.Client._Moffstation.GameObjects;
using Content.Shared._Moffstation.Cards.Components;
using Content.Shared._Moffstation.Strip.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Moffstation.Cards;

public sealed partial class CardHandVisualizerSystem : ManagedLayerVisualizerSystem<CardHandComponent>
{
    [Dependency] private readonly MetaDataSystem _meta = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CardHandComponent, GetHideInStripMenuEntityEvent>(HandOnGetHideInStripMenuEntity);
    }

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

        // Use the hand's contained cards' CURRENT sprites. This means that if the cards are face up / down, they will
        // appear that way in the hand as well.
        ApplyContainedCardLayersToHandSprite(component, visibleCards, c => c.CurrentSprite, layerFactory);
    }

    private void HandOnGetHideInStripMenuEntity(
        Entity<CardHandComponent> entity,
        ref GetHideInStripMenuEntityEvent args
    )
    {
        if (!AppearanceSystem.TryGetData<NetEntity[]>(entity, CardStackVisuals.Cards, out var visibleCards))
            return;

        var virtualHand = Spawn(null, args.SpawnAt);
        var meta = MetaData(entity);
        var virtualMeta = MetaData(virtualHand);
        _meta.SetEntityName(virtualHand, meta.EntityName, virtualMeta, raiseEvents: false);
        _meta.SetEntityDescription(virtualHand, meta.EntityDescription, virtualMeta);

        var virtHandSprite = new Entity<SpriteComponent?>(virtualHand, AddComp<SpriteComponent>(virtualHand));

        ApplyContainedCardLayersToHandSprite(
            entity.Comp,
            visibleCards,
            // Use the hand's contained cards' REVERSE sprites. This means that regardless of the cards' facing
            // directions, they will appear face down in the virtual hand.
            c => c.ReverseSprite,
            (_, layerData) =>
            {
                var idx = SpriteSystem.AddLayer(virtHandSprite, layerData, index: null);
                // ReSharper disable once RedundantAssignment // It's used by a debug assert, you piece.
                var gotLayer = SpriteSystem.TryGetLayer(virtHandSprite, idx, out var layer, logMissing: true);
                DebugTools.Assert(gotLayer);
                return layer ?? SpriteSystem.AddBlankLayer((virtHandSprite, virtHandSprite.Comp!));
            }
        );

        args.Entity = virtualHand;
    }

    private void ApplyContainedCardLayersToHandSprite(
        CardHandComponent component,
        NetEntity[] visibleCards,
        Func<CardComponent, PrototypeLayerData[]> cardLayers,
        Func<string, PrototypeLayerData, SpriteComponent.Layer> layerFactory
    )
    {
        var startingAngle = -(component.Angle / 2);
        var intervalAngle = visibleCards.Length != 1 ? component.Angle / (visibleCards.Length - 1) : 0;
        var startingXOffset = -(component.XOffset / 2);
        var intervalOffset = visibleCards.Length != 1 ? component.XOffset / (visibleCards.Length - 1) : 0;
        var layerScale = new Vector2(component.Scale, component.Scale);
        foreach (var (cardIndex, cardEnt) in GetEntityArray(visibleCards).Index())
        {
            if (!TryComp<CardComponent>(cardEnt, out var cardComp))
                continue;

            foreach (var (layerIndex, layerData) in cardLayers(cardComp).Index())
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
