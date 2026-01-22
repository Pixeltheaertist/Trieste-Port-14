namespace Content.Server._TP.Shuttles;

[RegisterComponent]
public sealed partial class AtmosphericThrusterComponent : Component
{
    [DataField]
    public bool Enabled = true;
}
