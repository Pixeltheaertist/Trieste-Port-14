using Content.Shared._TP.Kitchen;
using Robust.Client.GameObjects;

namespace Content.Client._TP14.Kitchen;

public sealed class DeepFriedVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SharedDeepFriedComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var comp, out var sprite))
        {
            var color = comp.CurrentFriedLevel switch
            {
                SharedDeepFriedComponent.FriedLevel.LightlyFried => Color.FromHex("#FFD580"),
                SharedDeepFriedComponent.FriedLevel.Fried => Color.FromHex("#954535"),
                SharedDeepFriedComponent.FriedLevel.Burnt => Color.FromHex("#0E0504"),
                _ => Color.White
            };

            // This is terrible. DON'T do this!
            if (sprite.Color != color)
                _sprite.SetColor((uid, sprite), color);
        }
    }
}
