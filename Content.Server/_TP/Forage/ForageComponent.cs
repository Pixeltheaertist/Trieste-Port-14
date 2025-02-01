using Content.Shared.EntityList;
using Robust.Shared.Prototypes;

namespace Content.Server._TP.Forage;

[RegisterComponent, Access(typeof(ForageSystem)), AutoGenerateComponentPause]
public sealed partial class ForageComponent : Component
{
    [DataField, ViewVariables]
    public Dictionary<string, ProtoId<EntityLootTablePrototype>>? Loot = new();

    /// <summary>
    /// Random shift of the appearing entity during gathering
    /// </summary>
    [DataField]
    public float GatherOffset = 0.3f;

    [DataField]
    public bool DestroyOnForage;

    /// <summary>
    /// Time in seconds to regrow produce for gather. Only has an effect if <see cref="DestroyOnForage"/> is false.
    /// </summary>
    [DataField]
    public TimeSpan RegrowTime = TimeSpan.FromSeconds(60);

    [DataField, ViewVariables(VVAccess.ReadOnly), AutoPausedField]
    public TimeSpan LastForagedTime;
}
