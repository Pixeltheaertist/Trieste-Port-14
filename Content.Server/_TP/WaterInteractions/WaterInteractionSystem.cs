using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared._TP.Jellids;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Fluids.Components;
using Content.Shared.Overlays;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.TP.Abyss.Components;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
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
    private float _timer;
    private const float NoiseTimer = 1f;
    private float _noisetimer;

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    private EntityUid? _soundEntity;

    public override void Update(float frameTime)
    {
        _timer += frameTime;
        _noisetimer += frameTime;

        if (_noisetimer >= NoiseTimer)
        {
            _noisetimer = 0f;
        }

        if (_timer >= UpdateTimer)
        {
            // FIRST: Collect ALL entities into a list to avoid enumeration issues
            var entitiesToProcess = new List<(EntityUid uid, InGasComponent inGas)>();

            var entities = EntityQueryEnumerator<InGasComponent>();
            while (entities.MoveNext(out var uid, out var inGas))
            {
                entitiesToProcess.Add((uid, inGas));
            }

            // NOW process the collected entities
            var entitiesToRemoveWaterViewer = new List<EntityUid>();
            var entitiesToAddWaterViewer = new List<EntityUid>();

            foreach (var (uid, inGas) in entitiesToProcess)
            {
                if (inGas.InWater)
                {
                    // Your water logic here

                    if (!HasComp<WaterViewerComponent>(uid))
                    {
                        entitiesToAddWaterViewer.Add(uid);
                    }
                }

                if (!TryComp<JellidComponent>(uid, out var jellid) &&
                    !TryComp<SiliconLawProviderComponent>(uid, out var borg))
                {
                    if (HasComp<WaterViewerComponent>(uid))
                    {
                        entitiesToRemoveWaterViewer.Add(uid);
                    }
                }

                if (TryComp<FlammableComponent>(uid, out var flame) && inGas.InWater)
                {
                    if (flame.OnFire)
                    {
                        flame.OnFire = false;
                    }
                }

                if (TryComp<Shared._TP.WaterInteractions.AbyssalProtectedComponent>(uid, out var abyssalProtected))
                {
                    continue;
                }

                if (inGas.InWater)
                {
                    if (inGas.CrushDepth < inGas.WaterAmount)
                    {
                        var damage = new DamageSpecifier
                        {
                            DamageDict = { ["Blunt"] = 35f }
                        };
                        _damageable.TryChangeDamage(uid, damage, origin: uid);
                    }
                }
            }

            // Apply component modifications
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
