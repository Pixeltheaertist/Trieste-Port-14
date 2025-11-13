using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared._TP.Jellids;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Overlays;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.TP.Abyss.Components;
using Robust.Server.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._TP.WaterInteractions;

/// <summary>
/// Water heavy. Lots of water hurt. Too much water makes person look like one of those hydraulic press videos on instagram.
/// In real terms, this system measures the "depth" of objects, and relates it to their designated crush depths.
/// If you are deeper than your crush depth and don't have an abyssal hardsuit on. Ruh roh.
/// </summary>
public sealed class WaterInteractionSystem : EntitySystem
{
    private const float UpdateTimer = 1f;
    private float _timer = 0f;
    private const float NoiseTimer = 1f;
    private float _Noisetimer = 0f;

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Update(float frameTime)
    {
        _timer += frameTime;
        _Noisetimer += frameTime;

        if (_Noisetimer >= NoiseTimer)
        {
            _Noisetimer = 0f;
        }

        if (_timer >= UpdateTimer)
        {
            // Create a snapshot of all entities to avoid collection modification during enumeration
            var entities = EntityManager.EntityQuery<InGasComponent>().ToList();

            // Collect entities that need component modifications
            var entitiesToRemoveWaterViewer = new List<EntityUid>();
            var entitiesToAddWaterViewer = new List<EntityUid>();

            // Check all objects affected by water
            foreach (var inGas in entities)
            {
                var uid = inGas.Owner;

                if (inGas.InWater)
                {
                    //if (TryComp<SolutionComponent>(uid, out var solution))
                    //  {

                    //  if (!_prototypeManager.TryIndex<ReagentPrototype>(solution.FloodReagent, out var water))
                    //   {
                    //        Log.Error("No component for the flooding water!");
                    //      return;
                    //   }

                    // if (!_solution.TryGetSolution(uid, solution.Solution, out _, out var actualSolution))
                    // {
                    //     return;
                    //   }

                    //   var FillAmount = actualSolution.Volume;

                    //   _solution.RemoveSolution(FillAmount);
                    //    _solution.AddReagent(water, water.ID, FillAmount);
                }


                // INSERT CONSTANT DEEP RUMBLE LOOP EXACTLY ONE SECOND IN LENGTH
                //  _audio.PlayPvs(inGas.RumbleSound, uid, AudioParams.Default.WithVolume(9f).WithMaxDistance(0.4f));
                //  Log.Info($"Rumbling audio for immersed entity {uid}");

                //   if (!TryComp<WaterBlockerComponent>(uid, out var blocker)) // If not wearing a mask eyes get hurt by water
                //   {
                // Instead of EnsureComp during iteration, add to list
                if (!HasComp<WaterViewerComponent>(uid))
                {
                    entitiesToAddWaterViewer.Add(uid);
                }

                //   }
                //    }
                //  else
                // {
                if (!TryComp<JellidComponent>(uid, out var jellid) &&
                    !TryComp<SiliconLawProviderComponent>(uid,
                        out var borg)) // If not a Jellid or borg, remove the water viewing resistance
                {
                    // Instead of removing immediately, add to list for later processing
                    if (HasComp<WaterViewerComponent>(uid))
                    {
                        entitiesToRemoveWaterViewer.Add(uid);
                    }
                }
                //   }

                if (TryComp<FlammableComponent>(uid, out var flame) && inGas.InWater)
                {
                    if (flame.OnFire) // Put out fire
                    {
                        flame.OnFire = false;
                    }
                }

                // Ignore those wearing abyssal hardsuits
                if (TryComp<AbyssalProtectedComponent>(uid, out var abyssalProtected))
                {
                    continue;
                }

                if (inGas.InWater)
                {
                    if (inGas.CrushDepth < inGas.WaterAmount)
                    {
                        // DIE.
                        var damage = new DamageSpecifier
                        {
                            DamageDict = { ["Blunt"] = 35f }
                        };
                        _damageable.TryChangeDamage(uid, damage, origin: uid);
                    }
                }
            }

            // Now safely process component modifications after iteration is complete
            foreach (var uid in entitiesToAddWaterViewer)
            {
                EnsureComp<WaterViewerComponent>(uid);
            }

            foreach (var uid in entitiesToRemoveWaterViewer)
            {
                _entityManager.RemoveComponent<WaterViewerComponent>(uid);
            }

            _timer = 0f;
        }
    }
}
