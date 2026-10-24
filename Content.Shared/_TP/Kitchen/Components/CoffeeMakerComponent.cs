using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._TP.Kitchen.Components;

[RegisterComponent]
public sealed partial class CoffeeMakerComponent : Component
{
    [DataField]
    public bool IsEnabled;

    [DataField]
    public float HeatingAmount = 6.5f;

    /// <summary>
    ///     The (fake) temperature the maker needs to reach before it brews.
    /// </summary>
    [DataField]
    public float BrewTemperature = 195f;

    /// <summary>
    ///     Which grounds reagent brews into which coffee reagent.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, ProtoId<ReagentPrototype>> Brews = new();

    /// <summary>
    ///     How much coffee is made per unit of grounds.
    /// </summary>
    [DataField]
    public FixedPoint2 BrewMultiplier = 2;

    [DataField]
    public SoundPathSpecifier? FinishSound = new("/Audio/_TP/Machines/Kitchen/coffee-pour.ogg");

    public TimeSpan? StartTime;

    public float CurrentHeat = 0.0f;
}

[Serializable, NetSerializable]
public enum CoffeeMakerVisuals : byte
{
    Basket,
    Beaker,
    Pitcher,
}
