namespace Content.Server._TP.Aberrant.Events;

public sealed class AberrantTriggerEvent
{
    /// <summary>
    ///     Entity that has triggered an aberrant effect.
    ///     Usually player, but can also be another object.
    /// </summary>
    public EntityUid? Target;
}

