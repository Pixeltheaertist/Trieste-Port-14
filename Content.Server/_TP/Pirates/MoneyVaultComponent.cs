using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Shared.TP.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MoneyVaultComponent : Component
{   // How much money is currently stored in the vault
    [DataField("vaultAmount")]
    public float VaultAmount = 0f;

    // How much money the greentext target is
    [DataField("targetAmount")]
    public float TargetAmount = 20000f;
}
