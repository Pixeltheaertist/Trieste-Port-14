namespace Content.Server._TP.Ladder;

/// <summary>
///     This component handles ladders.
/// </summary>
[RegisterComponent]
public sealed partial class LadderComponent : Component
{
    /// <summary>
    ///     What type of ladder this one is.
    ///     I.e., Up, Down, Tower Up, Tower Down, .etc
    /// </summary>
    [DataField]
    public string ThisSide = default!;

    /// <summary>
    ///     What the other side of the ladder is.
    ///     If 'ThisSide' is 'up', the target would be 'down', and vice versa.
    /// </summary>
    [DataField]
    public string TargetSide = default!;
}
