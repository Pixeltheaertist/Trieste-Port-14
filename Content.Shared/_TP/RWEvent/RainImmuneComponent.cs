using Robust.Shared.GameStates;

namespace Content.Shared.Weather;

/// <summary>
/// This entity will mark objects that are NOT crushable by The Rain.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RainImmuneComponent : Component
{

}
