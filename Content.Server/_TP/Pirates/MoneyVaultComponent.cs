using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Shared.TP.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MoneyVaultComponent : Component
{
    [DataField("VaultAmount")]
    public float VaultAmount = 0f;
}
