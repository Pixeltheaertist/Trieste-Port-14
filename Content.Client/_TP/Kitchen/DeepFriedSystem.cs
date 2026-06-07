using Content.Shared._TP.Kitchen.Components;
using Content.Shared._TP.Kitchen.Events;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;

namespace Content.Client._TP.Kitchen;

/// <summary>
///     Client-side sprite visualizer system for the deep-fried item component.
///     Created by Cookie (FatherCheese) for Trieste Port 14.
/// </summary>
public sealed class DeepFriedSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeepFriedComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<DeepFriedComponent, DeepFriedLevelChangedEvent>(OnFriedLevelChanged);
        SubscribeLocalEvent<DeepFriedComponent, EntGotRemovedFromContainerMessage>(OnRemovedFromContainer);
    }

    /// <summary>
    ///     Raised when the fried item is removed from a container.
    /// </summary>
    /// <param name="friedEnt"></param>
    /// <param name="args"></param>
    private void OnRemovedFromContainer(Entity<DeepFriedComponent> friedEnt, ref EntGotRemovedFromContainerMessage args)
    {
        UpdateSprite(friedEnt.Owner, friedEnt.Comp);
    }

    /// <summary>
    ///     Raised when the fried level changes.
    /// </summary>
    /// <param name="friedEnt">SharedDeepFriedComponent entity</param>
    /// <param name="args">DeepFriedLevelChangedEvent arguments</param>
    private void OnFriedLevelChanged(Entity<DeepFriedComponent> friedEnt, ref DeepFriedLevelChangedEvent args)
    {
        UpdateSprite(friedEnt.Owner, friedEnt.Comp);
    }

    /// <summary>
    ///     Raised the component is added to an entity.
    /// </summary>
    /// <param name="friedEnt">SharedDeepFriedComponent entity</param>
    /// <param name="args">ComponentStartup arguments</param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnComponentStartup(Entity<DeepFriedComponent> friedEnt, ref ComponentStartup args)
    {
        UpdateSprite(friedEnt.Owner, friedEnt.Comp);
    }

    /// <summary>
    ///     Updates the sprite color based on the fried level.
    /// </summary>
    /// <param name="friedUid">SharedDeepFriedComponent entity uid</param>
    /// <param name="friedComp">SharedDeepFriedComponent entity</param>
    private void UpdateSprite(EntityUid friedUid, DeepFriedComponent friedComp)
    {
        // Simple enough - we check if we have a sprite (we always should),
        // and then we set the color based on the fried level from the component.
        if (!TryComp<SpriteComponent>(friedUid, out var sprite))
            return;

        var color = friedComp.CurrentFriedLevel switch
        {
            DeepFriedComponent.FriedLevel.LightlyFried => Color.FromHex("#FFD580"),
            DeepFriedComponent.FriedLevel.Fried => Color.FromHex("#954535"),
            DeepFriedComponent.FriedLevel.Burnt => Color.FromHex("#0E0504"),
            _ => Color.White
        };

        _sprite.SetColor((friedUid, sprite), color);
    }
}
