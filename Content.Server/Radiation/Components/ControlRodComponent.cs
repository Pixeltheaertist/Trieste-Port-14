using Content.Server.Radiation.Systems;
namespace Content.Server.Radiation.Components;
/// <summary>
///     Blocks radiation when placed on tile.
/// </summary>
[RegisterComponent]
public sealed partial class ControlRodComponent : Component
{

    /// <summary>
    ///     This value is multiplied by the current extension value to produce the rads blocked
    /// </summary>
    [DataField("shutdownRange")]
    public float ShutdownRange = 0.8f;

    [DataField("controlRange")]
    public float ControlRange = 0.2f;
    /// <summary>
    ///     The current range that the rod is currently extended out too (may not drop below 0)
    /// </summary>
    [DataField("currentExtension")]
    public float CurrentExtension = 0f;
    /// <summary>
    ///     The maximum range that the rod is can be extended out too
    /// </summary>
    [DataField("maxExtension")]
    public float MaxExtension = 1f;
    /// <summary>
    ///     The range intervals by which the range can be extended or retracted
    /// </summary>
    [DataField("extensionStep")]
    public float ExtensionStep = 0.1f;
    //Moderators values won't happen for now - atm rods only block rads rather than altering intensity
    /// <summary>
    ///     The moderator value of the rod if the rod has a moderator - can be used to increase reactivity
    /// </summary>
    //[DataField("moderatorValue")]
    //public float ModeratorValue = 1f;
    /// <summary>
    ///     Moderator range - should be divisible by the extension step and less than the max extension
    /// </summary>
    //[DataField("moderatorRange")]
    //public float ModeratorRange = 0f;
    [ViewVariables]
    public EntityUid? ConnectedConsole;
    public const string ControlRodPort = "ControlRodReceiver";
    /// <summary>
    ///     Amount of time it takes a command to execute
    /// </summary>
    [DataField("commandTime")]
    public float CommandTime = 0.5f;
    //timer to track commands
    public float Timer = 0f;
}
