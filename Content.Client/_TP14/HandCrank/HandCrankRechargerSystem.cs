using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Client._TP14.HandCrank;

public partial class HandCrankRechargerSystem : SharedHandCrankRechargerSystem
{
    // All logic done in server. Only here for prediction
    protected override void StartDoAfter(EntityUid uid, EntityUid user, HandCrankRechargerComponent component) {}
}
