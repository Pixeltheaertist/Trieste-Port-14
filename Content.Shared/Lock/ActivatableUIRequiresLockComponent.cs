using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Lock;

/// <summary>
/// This is used for activatable UIs that require the entity to have a lock in a certain state.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(LockSystem))]
public sealed partial class ActivatableUIRequiresLockComponent : Component
{
    /// <summary>
    /// TRUE: the lock must be locked to access the UI.
    /// FALSE: the lock must be unlocked to access the UI.
    /// </summary>
    [DataField]
    public bool RequireLocked;

    /// <summary>
    /// Sound to be played if an attempt is blocked.
    /// </summary>
    [DataField]
    public SoundSpecifier? AccessDeniedSound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");
}
