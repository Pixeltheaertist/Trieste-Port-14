using System.Linq;
using Content.Client._Moffstation.GameObjects;
using Content.Shared._Moffstation.Cards.Components;
using Content.Shared._Moffstation.Strip.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace Content.Client._Moffstation.Cards;

public sealed class CardVisualizerSystem : ManagedLayerVisualizerSystem<CardComponent>
{
    [Dependency] private readonly MetaDataSystem _meta = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CardComponent, GetHideInStripMenuEntityEvent>(CardOnGetHideInStripMenuEntity);
    }

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

    private void CardOnGetHideInStripMenuEntity(Entity<CardComponent> entity, ref GetHideInStripMenuEntityEvent args)
    {
        var virtualCard = Spawn(null, args.SpawnAt);
        var meta = MetaData(entity);
        var virtualMeta = MetaData(virtualCard);
        _meta.SetEntityName(virtualCard, meta.EntityName, virtualMeta, raiseEvents: false);
        _meta.SetEntityDescription(virtualCard, meta.EntityDescription, virtualMeta);

        var sprite = new Entity<SpriteComponent?>(virtualCard, AddComp<SpriteComponent>(virtualCard));
        foreach (var layer in entity.Comp.ReverseSprite)
        {
            SpriteSystem.AddLayer(sprite, layer, index: null);
        }

        args.Entity = virtualCard;
    }
}
