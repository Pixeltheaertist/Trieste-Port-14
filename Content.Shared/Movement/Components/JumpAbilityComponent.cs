using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// A component for configuring the settings for the jump action.
/// To give the jump action to an entity use <see cref="ActionGrantComponent"/> and <see cref="ItemActionGrantComponent"/>.
/// The basic action prototype is "ActionGravityJump".
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JumpAbilityComponent : Component
{
    /// <summary>
    /// How far you will jump (in tiles).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float JumpDistance = 1f;

    /// <summary>
    /// Basic “throwing” speed for TryThrow method.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float JumpThrowSpeed = 5f;

    /// <summary>
    /// This gets played whenever the jump action is used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? JumpSound;
}

public sealed partial class GravityJumpEvent : InstantActionEvent;
