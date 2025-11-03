using Content.Shared._TP.Kitchen;
using Robust.Client.GameObjects;

namespace Content.Client._TP14.Kitchen;

public sealed class DeepFriedVisualizerSystem : EntitySystem
{

    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedDeepFriedComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(Entity<SharedDeepFriedComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out _))
            return;

        var fryColor = ent.Comp.CurrentFriedLevel switch
        {
            SharedDeepFriedComponent.FriedLevel.LightlyFried => Color.FromHex("#C4A484FF"),
            SharedDeepFriedComponent.FriedLevel.Fried => Color.FromHex("#964B00FF"),
            SharedDeepFriedComponent.FriedLevel.Burnt => Color.Black,
            _ => Color.White,
        };

        _sprite.SetColor(ent.Owner, fryColor);
    }
}
