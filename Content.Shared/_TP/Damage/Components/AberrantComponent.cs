namespace Content.Shared._TP.Damage.Components;

/// <summary>
/// Component for entities that take aberrant damage
/// </summary>
[RegisterComponent]
public sealed partial class AberrantComponent : Component
{
    /// <summary>
    /// The amount of aberrant damage required for each tier of effect
    /// should contain 3 items, for low, medium, and high damage respectively
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public List<float> Thresholds = new List<float>{50, 100, 200};

    [ViewVariables(VVAccess.ReadWrite)]
    public float AberrantDamage = 0;
}
