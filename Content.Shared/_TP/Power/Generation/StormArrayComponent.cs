using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._TP.Power.Generation;

/// <summary>
///     Component for the storm array.
///     Created by Cookie (Father Cheese) for Trieste Port 14.
/// </summary>
[RegisterComponent]
public sealed partial class StormArrayComponent : Component
{
    #region Announcements

    [DataField]
    public bool FirstAnnouncement;

    [DataField]
    public bool SecondAnnouncement;

    [DataField]
    public bool ThirdAnnouncement;

    [DataField]
    public bool FourthAnnouncement;

    [DataField]
    public bool FifthAnnouncement;

    public string StatusMessage;

    #endregion

    #region Cooling

    [DataField]
    public float CoolingEfficiency = 0.8F;

    /// <summary>
    ///     Whether the storm array is currently active and generating power.
    /// </summary>
    [DataField]
    public bool IsActive;

    #endregion

    #region AtmosStorage

    [ViewVariables]
    public float LastCoolingRate = 0.0F;

    [ViewVariables]
    public float LastCoolantFlow = 0.0F;

    #endregion

}

[Serializable, NetSerializable]
public enum StormArrayVisuals : byte
{
    Idle,
    Active,
}

[Serializable, NetSerializable]
public sealed partial class StormArrayDoAfterEvent : SimpleDoAfterEvent;
