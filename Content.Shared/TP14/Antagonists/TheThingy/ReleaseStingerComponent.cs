using Content.Shared.Actions;
using Content.Shared.Ninja.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Thingy.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedItemCreatorSystem))]
public sealed partial class ReleaseStingerComponent : Component
{
    /// <summary>
    /// The action id for creating an item.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<InstantActionComponent> Action = string.Empty;

    /// <summary>
    /// The action id for pulling back the stinger.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<InstantActionComponent> PullBackAction = string.Empty;

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Item to create with the action
    /// </summary>
    [DataField(required: true)]
    public EntProtoId SpawnedPrototype = string.Empty;

    /// <summary>
    /// The entity that represents the stinger (for pulling it back).
    /// </summary>
    [DataField]
    public EntityUid? SpawnedStingerEntity = null;
}

public sealed partial class CreateStingerEvent : InstantActionEvent;

/// <summary>
/// Action event to pull back the stinger.
/// </summary>
public sealed partial class PullBackStingerEvent : InstantActionEvent;
