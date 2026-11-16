namespace Content.Server._TP.Objectives;

/// <summary>
/// Objective condition: the target must be alive, cuffed and within <see cref="Range"/> of the objective holder.
/// The target is picked by the objective's TargetObjective component.
/// </summary>
[RegisterComponent, Access(typeof(HostageConditionSystem))]
public sealed partial class HostageConditionComponent : Component
{
    /// <summary>
    /// How close, in tiles, the hostage has to be to the objective holder.
    /// </summary>
    [DataField]
    public float Range = 5f;
}
