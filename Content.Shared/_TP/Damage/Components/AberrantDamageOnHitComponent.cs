namespace Content.Shared._TP.Damage.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class AberrantDamageOnHitComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float Amount = 10;
}
