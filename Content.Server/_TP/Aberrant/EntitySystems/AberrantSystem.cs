using Content.Server._TP.Aberrant.Components;
using Content.Server._TP.Aberrant.Events;
using Content.Shared._TP.Damage.Components;

namespace Content.Server._TP.Aberrant.EntitySystems;

/// <summary>
/// This handles...
/// </summary>
public sealed class AberrantSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
    }

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
                // select event
                RaiseLocalEvent(new AberrantTriggerEvent(uid));
            }
            else if (aberrant.AberrantDamage >= aberrant.Thresholds[1])
            {
                //run medium tier event
                RaiseLocalEvent(new AberrantTriggerEvent(uid));
            }
            else if (aberrant.AberrantDamage >= aberrant.Thresholds[0])
            {
                //run low tier event
                AddComp<AberrantEffectForceSpeechComponent>(uid);
                RaiseLocalEvent(new AberrantTriggerEvent(uid));
            }
        }
    }
    private void CleanUp(EntityUid uid)
    {
        //Removes any aberrant effects that are no longer needed

    }
}
