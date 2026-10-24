using Robust.Shared.Prototypes;

namespace Content.Shared._TP.Kitchen.Components;

[RegisterComponent]
public sealed partial class CoffeeFilterComponent : Component
{
    [DataField]
    public bool InfiniteUses;

    /// <summary>
    ///     Whether this filter is used up. Dirty filters can't be brewed with.
    /// </summary>
    [DataField]
    public bool IsDirty;

    /// <summary>
    ///     What this filter gets replaced with after a brew. Ignored if <see cref="InfiniteUses"/> is set.
    /// </summary>
    [DataField]
    public EntProtoId? DirtyPrototype;
}
