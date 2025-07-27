using Content.Shared._TP.Damage.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._TP.Damage.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class AberrantSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AberrantDamageOnHitComponent, MeleeHitEvent>(OnMeleeHit);
        //SubscribeLocalEvent<>();
    }
    // do something on update
    public override void Update(float frameTime)
    {
        RunAberrantEvents();
    }

    private void RunAberrantEvents()
    {
        // get all entities with an aberrant component, and iterate through them
        var enumerator = EntityQueryEnumerator<AberrantComponent>();
        while (enumerator.MoveNext(out var uid, out var aberrant))
        {
            //check for an aberrant event from highest tier down. Only run for the highest tier that applies
            if (aberrant.AberrantDamage >= aberrant.Thresholds[2])
            {
                //run highest tier event
            }
            else if (aberrant.AberrantDamage >= aberrant.Thresholds[1])
            {
                //run medium tier event
            }
            else if (aberrant.AberrantDamage >= aberrant.Thresholds[0])
            {
                //run low tier event
            }
        }
    }

    private void OnMeleeHit(EntityUid uid, AberrantDamageOnHitComponent component, MeleeHitEvent args)
    {
        // get the entity that was hit
        var targets = args.HitEntities;
        foreach (var entity in targets)
        {
            TryChangeAberrant(entity, component.Amount);
        }
    }

    private void ChangeAberrant(AberrantComponent component, float amount)
    {
        component.AberrantDamage += amount;
    }

    public void TryChangeAberrant(EntityUid uid, float amount)
    {
        if(EntityManager.TryGetComponent(uid, out AberrantComponent? aberrant))
        {
            ChangeAberrant(aberrant, amount);
        }
    }
}
