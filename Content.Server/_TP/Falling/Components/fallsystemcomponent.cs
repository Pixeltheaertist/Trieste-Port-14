namespace Content.Server._TP.Falling.Components;

[RegisterComponent]
public sealed partial class FallSystemComponent : Component
{
    public float MaxRandomRadius { get; set; } = 20.0f; // Decides the random teleport of the FallSystem
}
