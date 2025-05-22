using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Shared.Camera;
using Content.Shared.Weather;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server._TP.RWEvent;

public sealed class DeathRainSystem : EntitySystem
{

    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private const float UpdateInterval = 20f;
    private const float RumbleInterval = 10;

    private float _updateTimer = 0f;


    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        //_updateTimer += frameTime;

        if (_updateTimer >= RumbleInterval)
        {

          foreach (var rumbler in EntityManager.EntityQuery<RainCrushableComponent>())
            {
                var rumble = rumbler.Owner;
                var kick = new Vector2(_random.NextFloat(), _random.NextFloat()) * 2f;
                _sharedCameraRecoil.KickCamera(rumble, kick);
                _audio.PlayPvs("/Audio/Ambience/Objects/gravity_gen_hum.ogg", rumble);
            }
        }

        if (_updateTimer >= UpdateInterval)
        {
            _updateTimer = 0f;

            // In shelter?
            foreach (var entity in EntityManager.EntityQuery<RainCrushableComponent>())
            {
                var entityUid = entity.Owner;

                if (TryComp<RainImmuneComponent>(entityUid, out var immune))
                {
                    // This creature is innately immune to rain. Spared.
                     continue;
                }

                var shelters = GetEntityQuery<RainShelterComponent>();
                foreach (var shelter in _lookup.GetEntitiesInRange(entityUid, 1f))
                {
                     Log.Info("Found shelter");
                     if (shelters.HasComponent(shelter))
                     {
                         Log.Info("Inside shelter");
                         return;
                     }
                }

                // Not in shelter. Bye bye. Say hi to the void for me.

                // _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("meltdown-alert-warning"), component.title, announcementSound: component.MeltdownSound, colorOverride: component.Color);
                QueueDel(entityUid);
                }
            }
          }
      }
