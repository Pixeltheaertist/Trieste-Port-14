using System.Linq.Expressions;
using Linguini.Syntax.Ast;
using Robust.Shared.GameStates;


namespace Content.Shared.Explosion.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class ExplosivePowerSelectorComponent : Component
    {
        [DataField] public float ExplosivePower = 1f;

        [DataField] public List<float>? ExplosivePowerOptions = null;


    }
}
