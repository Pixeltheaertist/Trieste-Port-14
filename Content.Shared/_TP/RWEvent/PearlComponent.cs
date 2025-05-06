namespace Content.Server._TP;

[RegisterComponent]
public sealed partial class PearlComponent : Component
{
    [DataField("pearlMessage")]
    public string PearlMessage = "pearl-message-BASE";
}
