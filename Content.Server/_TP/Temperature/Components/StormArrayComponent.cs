namespace Content.Server._TP.Temperature.Components;

/// <summary>
///     Component for the storm array.
///     Created by Cookie (Father Cheese) for Trieste Port 14.
/// </summary>
[RegisterComponent]
public sealed partial class StormArrayComponent : Component
{
    #region Announcements

    [DataField]
    public bool FirstAnnouncement = false;

    [DataField]
    public bool SecondAnnouncement = false;

    [DataField]
    public bool ThirdAnnouncement = false;

    [DataField]
    public bool FourthAnnouncement = false;

    [DataField]
    public bool FifthAnnouncement = false;

    #endregion

    #region Cooling

    [DataField]
    public int MaxCoolingRate = 50000;

    [DataField]
    public float CoolingEfficiency = 0.8F;

    #endregion

    #region AtmosStorage

    [ViewVariables]
    public float LastCoolingRate = 0.0F;

    [ViewVariables]
    public float LastCoolantFlow = 0.0F;

    [ViewVariables]
    public float LastPressureDelta = 0.0F;

    #endregion

}
