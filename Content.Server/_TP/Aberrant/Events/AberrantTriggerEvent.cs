using JetBrains.Annotations;

namespace Content.Server._TP.Aberrant.Events;
[PublicAPI]
public sealed class AberrantTriggerEvent
{
    /// <summary>
    ///     Entity that has triggered an aberrant effect.
    ///     Usually player, but can also be another object.
    /// </summary>
    public EntityUid Target;

    public AberrantTriggerEvent(EntityUid target)
    {
        Target = target;
    }
}
