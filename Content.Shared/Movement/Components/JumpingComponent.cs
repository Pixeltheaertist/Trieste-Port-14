namespace Content.Shared.Movement.Components;

[RegisterComponent]
public sealed partial class JumpingComponent : Component
{
    [DataField]
    public bool IsJumping = false;

    [DataField]
    public bool WasJumping = false;

    [DataField]
    public int JumpTime = 5;

    [DataField]
    public TimeSpan LastJumped = new TimeSpan();
}
