// Taken from https://github.com/emberfall-14/emberfall/pull/4/files with permission

using Content.Shared.TP14.Bell.Systems;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.TP14.Bell.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedBellConsoleSystem))]
[AutoGenerateComponentState]
public sealed partial class BellConsoleComponent : Component
{
    /// <summary>
    /// The tag used to identify preferred docking airlocks at the destination.
    /// The elevator will attempt to dock at airlocks with this tag first.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TagPrototype> DockTag;

    /// <summary>
    /// Defines which elevator platforms this console can control.
    /// Used to ensure consoles can only control their assigned platforms.
    /// The whitelist should match exactly one elevator platform.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist PlatformWhitelist = new();

    /// <summary>
    /// The current elevator platform this console is controlling.
    /// Found by checking nearby platforms against <see cref="PlatformWhitelist"/>.
    /// </summary>
    [DataField]
    public EntityUid? Platform;

    /// <summary>
    /// Tracks whether a valid platform connection exists on the server.
    /// This is networked separately from Platform since clients cannot
    /// reference platforms outside their PVS range.
    /// </summary>
    /// <remarks>
    /// Used by the UI to show connection status and enable/disable controls.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public bool HasPlatform;
}
