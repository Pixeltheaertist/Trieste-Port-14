namespace Content.Server._TP.Aberrant.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class AberrantEffectGasSpawnComponent : Component
{
    /// <summary>
    /// Gas to spawn. Seawater is default.
    /// </summary>
    [DataField]
    public int GasId = 9;

    /// <summary>
    /// Mols of gas to spawn.
    /// </summary>
    [DataField]
    public float Volume = 100f;
}
