using Robust.Shared.GameStates;

namespace Content.Shared.Weather;

/// <summary>
/// This entity will protect players from The Rain if they're on top of it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RainShelterComponent : Component
{

}
