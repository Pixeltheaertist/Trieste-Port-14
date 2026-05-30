using Content.Shared._TP.Damage.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._TP.Damage.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class AberrantDamageSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AberrantDamageOnHitComponent, MeleeHitEvent>(OnMeleeHit);
        //SubscribeLocalEvent<>(); - when chat refactor happens can easily add an event for chat hearing
    }
    // do something on update


    private void OnMeleeHit(EntityUid uid, AberrantDamageOnHitComponent component, MeleeHitEvent args)
    {
        // get the entity that was hit
        var targets = args.HitEntities;
        foreach (var entity in targets)
        {
            TryChangeAberrant(entity, component.Amount);
        }
    }

    public void TryChangeAberrant(EntityUid uid, float amount)
    {
        if(EntityManager.TryGetComponent(uid, out AberrantComponent? aberrant))
        {
            ChangeAberrant(uid, aberrant, amount);
        }
    }
    private void ChangeAberrant(EntityUid uid, AberrantComponent component, float amount)
    {
        component.AberrantDamage += amount;
        if (component.AberrantDamage < component.Thresholds[0])
        {
            //raise cleanup event

        }
    }


}
