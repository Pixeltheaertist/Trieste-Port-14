using Content.Server.Storage.Components;
using Content.Shared.EntityList;
using Robust.Shared.Prototypes;

namespace Content.Shared._TP;

[RegisterComponent]
public partial class JumpingComponent : Component
{
    [DataField]
    public bool IsJumping = false;

    [DataField]
    public int JumpTime = 5;

    [DataField]
    public TimeSpan LastJumped = new TimeSpan();
}
