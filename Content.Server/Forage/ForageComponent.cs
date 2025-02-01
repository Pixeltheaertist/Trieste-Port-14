using Content.Shared.EntityList;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Forage;

[RegisterComponent, Access(typeof(ForageSystem))]
public sealed partial class ForageComponent : Component
{
    [DataField, ViewVariables]
    public Dictionary<string, ProtoId<EntityLootTablePrototype>>? Loot = new();

    /// <summary>
    /// Random shift of the appearing entity during gathering
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float GatherOffset = 0.3f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool DestroyOnForage;

    /// <summary>
    /// Time in seconds to regrow produce for gather. Only has an effect if <see cref="DestroyOnForage"/> is true.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float RegrowTime = 60.0f;

    [ViewVariables(VVAccess.ReadOnly)]
    public float TimeSinceForage;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Regrowing;
}
