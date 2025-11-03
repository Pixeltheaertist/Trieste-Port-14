namespace Content.Shared._TP.Kitchen;

[RegisterComponent]
public sealed partial class SharedDeepFriedComponent : Component
{
    [DataField]
    public FriedLevel CurrentFriedLevel = FriedLevel.None;

    public enum FriedLevel
    {
        LightlyFried,
        Fried,
        Burnt,
        None,
    }
}
