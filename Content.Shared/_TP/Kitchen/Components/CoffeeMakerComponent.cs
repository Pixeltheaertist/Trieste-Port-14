using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._TP.Kitchen.Components;

[RegisterComponent]
public sealed partial class CoffeeMakerComponent : Component
{
    [DataField]
    public bool IsEnabled;

    [DataField]
    public float HeatingAmount = 6.5f;

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
