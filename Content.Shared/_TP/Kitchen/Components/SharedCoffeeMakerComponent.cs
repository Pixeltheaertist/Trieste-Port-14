using Robust.Shared.Serialization;

namespace Content.Shared._TP.Kitchen.Components;

[RegisterComponent]
[ComponentProtoName("CoffeeMaker")]
public sealed partial class SharedCoffeeMakerComponent : Component
{
    [DataField]
    public bool IsEnabled;

    [DataField]
    public float HeatingAmount = 6.5f;

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
