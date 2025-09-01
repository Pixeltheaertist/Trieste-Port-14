using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Ghost;
using Content.Server.Nuke;
using Content.Shared.Light.Components;
using Content.Shared.Radiation.Components;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Random;

namespace Content.Server._TP.Events.Systems;

/// <summary>
/// This handles reactor events like light flickering and global announcements for the floatsam event.
/// </summary>
public sealed class EventReactorSystem : EntitySystem
{

    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;


    private float _updateTimer = 0f;
    private float _flickerTimer = 0f;
    private const float UpdateInterval = 1f;
    private const float FlickerInterval = 100f;

    /// <inheritdoc/>
    public override void Initialize()
    {
    }

     public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _updateTimer += frameTime;

            _flickerTimer += frameTime;

            if (_updateTimer >= UpdateInterval)
            {
                foreach (var entity in EntityManager.EntityQuery<Components.EventReactorComponent>())
                {
                    entity.RemainingTime -= frameTime;
                    EntityUid uid = entity.Owner;
                    var component = EntityManager.GetComponent<Components.EventReactorComponent>(uid);
                    ReactorCheck(uid, component);
                }
                _updateTimer = 0f;
            }

            if (_flickerTimer >= FlickerInterval)
            {
                foreach (var entity in EntityManager.EntityQuery<Components.EventReactorComponent>())
                {
                    EntityUid uid = entity.Owner;
                    FlickerReactor(uid, entity);
                }
                _flickerTimer = 0f;
            }
        }


     private void ReactorCheck(EntityUid uid, Components.EventReactorComponent component)
     {

            if (component is { RemainingTime: <= 3600, FirstWarning: false })
            {
                 _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("first-alert-warning"), component.title, announcementSound: component.Sound, colorOverride: component.Color);
                component.FirstWarning = true;
            }

            if (component is { RemainingTime: <= 2600, SecondWarning: false })
            {
                 _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("second-alert-warning"), component.title, announcementSound: component.Sound, colorOverride: component.Color);
                  EnsureComp<RadiationSourceComponent>(uid);
                component.SecondWarning = true;
            }

            if (component is { RemainingTime: <= 1800, ThirdWarning: false })
            {
                 _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("third-alert-warning"), component.title, announcementSound: component.Sound, colorOverride: component.Color);
                component.ThirdWarning = true;
            }

            if (component is { RemainingTime: <= 30, MeltdownWarning: false })
            {
                 _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("meltdown-alert-warning"), component.title, announcementSound: component.MeltdownSound, colorOverride: component.Color);
                component.MeltdownWarning = true;
            }

            if (component is { RemainingTime: <= 0 })
            {
                _explosion.QueueExplosion(uid, "Default", 5000000, 5, 100);
                RaiseLocalEvent(new NukeExplodedEvent());
                QueueDel(uid);
            }
    }

     private void FlickerReactor(EntityUid uid, Components.EventReactorComponent component)
     {
        var lights = GetEntityQuery<PoweredLightComponent>();
        foreach (var light in _lookup.GetEntitiesInRange(uid, component.Radius, LookupFlags.StaticSundries ))
        {
            if (!lights.HasComponent(light))
                continue;

            if (!_random.Prob(component.FlickerChance))
                continue;

            _ghost.DoGhostBooEvent(light);
        }
    }
}
