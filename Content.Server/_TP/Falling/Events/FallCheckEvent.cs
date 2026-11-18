namespace Content.Server._TP.Falling.Events;

/// <summary>
/// Queued to re-check whether an entity should fall, for cases the parent change alone can't catch.
/// </summary>
public sealed class FallCheckEvent(EntityUid entity) : EntityEventArgs
{
    public readonly EntityUid Entity = entity;
}
