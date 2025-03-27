using Content.Server._TP.Aberrant.Components;
using Content.Server._TP.Aberrant.Events;
using Content.Shared.Popups;

namespace Content.Server._TP.Aberrant.EntitySystems;

/// <summary>
/// This handles...
/// </summary>
public sealed class AberrantFeelingSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AberrantFeelingComponent, AberrantTriggerEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, AberrantFeelingComponent component, AberrantTriggerEvent args)
    {

    }
}
