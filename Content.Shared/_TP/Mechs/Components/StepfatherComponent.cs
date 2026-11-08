using Content.Shared.Alert;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._TP.Mechs.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StepfatherComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> BatteryAlert = "BorgBattery";

    [DataField]
    public ProtoId<AlertPrototype> NoBatteryAlert = "BorgBatteryNone";

    [DataField]
    public float ResetDrawTime;

    [DataField]
    public bool IsSubverted;

    [DataField]
    public float DrainAccumulator;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? PilotWhitelist;

    [DataField]
    public float MechToPilotDamageMultiplier;

    /// <summary>
    ///     Played when a pilot climbs into the Rotund.
    /// </summary>
    [DataField]
    public SoundSpecifier? EnterSound = new SoundPathSpecifier("/Audio/Mecha/mechmove03.ogg");
}
