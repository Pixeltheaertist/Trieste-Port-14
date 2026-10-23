using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
///     Allows energy guns to be recharged by cranking!
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HandCrankRechargerComponent : Component
{
    /// <summary>
    ///     Amount of charge that is added to the battery per crank in joules.
    /// </summary>
    [DataField]
    public float AmountRechargedPerCrank = 20f;

    /// <summary>
    ///     Maximum charge that the crank can crank up to in joules!
    /// </summary>
    [DataField]
    public float? MaximumChargeByCrank;

    /// <summary>
    ///     Time it takes to crank the weapon one time.
    /// </summary>
    [DataField]
    public TimeSpan TimeToCrank = TimeSpan.FromSeconds(20);

    /// <summary>
    ///     Sound of the crank!
    /// </summary>
    [DataField]
    public SoundSpecifier? CrankSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/shotgun_close.ogg");
}

/// <summary>
///     Doafter for recharging cranking weapons.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class GunCrankAfterEvent : SimpleDoAfterEvent;
