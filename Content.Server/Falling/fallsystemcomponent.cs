namespace Content.Server.Falling;

//Summary
// Anything with this component will be affected by Trieste gravity and falling, see FallSystem.cs.
//Summary
[RegisterComponent]
public sealed partial class FallSystemComponent : Component
{
 public float MaxRandomRadius { get; set; } = 20.0f; // Decides the random teleport of the fallsystem
}
