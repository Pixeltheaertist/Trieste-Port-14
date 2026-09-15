using Content.Server._TP.Aberrant.Components;
using Content.Server._TP.Aberrant.Events;
using Content.Shared.Popups;

namespace Content.Server._TP.Aberrant.EntitySystems;

/// <summary>
/// This handles...
/// </summary>
public sealed class AberrantEffectFeelingSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AberrantEffectFeelingComponent, AberrantTriggerEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, AberrantEffectFeelingComponent component, AberrantTriggerEvent args)
    {

    }
}
