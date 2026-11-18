using Content.Shared.Roles.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._TP.NanoTraitor;

/// <summary>
/// Added to mind role entities to tag that they are a NanoTrasen traitor.
/// These minds also have <see cref="TraitorRoleComponent"/>, so upstream traitor objectives still work;
/// use this to tell NanoTraitors apart from Syndicate traitors.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NanoTraitorRoleComponent : BaseMindRoleComponent;
