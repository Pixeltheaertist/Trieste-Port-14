using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Shared.Weather;
using Content.Server._TP;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Server.Chat.Systems;
using Content.Server.Event.Systems;
using Robust.Shared.Timing;
using Content.Shared.TP14.Bell.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Server.Audio;
using Content.Server.Weather;
using Content.Shared.Audio;
using Content.Shared.GameTicking;
using Content.Shared.Gravity;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Serilog;
using Content.Server.Parallax;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Layers;
using Content.Shared.Parallax.Biomes.Markers;



//Summary
// This system is a simple event that will cause a weather change to occur during the event, and shift to another at the end.
// The "TargetWeather" variable will dictate what weather prototype is active during the event.
// The "ReturnWeather" variable will dictate what weather it returns to after the event (this is permanent until changed again!)
// The "Lightning" variable dictates whether or not lightning is allowed to occurr during this event, or if it should be disabled. On by default.
// The "Sunlight" variable dicatates whether the platform and waste zone will be fully covered in "light"
// The "SunlightColor" variable takes a hex code to indicate a specific color to bathe the platform in if "Sunlight" is enabled.
//Summary

namespace Content.Server.StationEvents.Events
{
    public sealed class WeatherChangeRule : StationEventSystem<WeatherChangeRuleComponent>
    {

        [Dependency] private readonly GhostSystem _ghost = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedGameTicker _gameTicker = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedWeatherSystem _weather = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;


        protected override void Started(EntityUid uid, WeatherChangeRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
        {
            base.Started(uid, comp, gameRule, args);

            foreach (var weather in EntityManager.EntityQuery<WeatherComponent>())
            {
                var target = weather.Owner;
                if (!_prototypeManager.TryIndex<WeatherPrototype>(comp.TargetWeather, out var targetWeather))
                {
                    Log.Error("Weather prototype not found!");
                    return;
                }

                var mapId = Transform(target).MapID;
                var mapUid = Transform(target).MapUid;

                _weather.SetWeather(mapId, targetWeather, TimeSpan.FromMinutes(99999));

                if (comp.Sunlight)
                {
                    if (mapUid.HasValue)
                    {
                        var realMapUid = mapUid.Value;

                        // EnsureComp<MapGridComponent>(realMapUid);
                        EnsureComp<MetaDataComponent>(realMapUid);


                        if (!TryComp<MetaDataComponent>(mapUid, out var metadata))
                        {
                            Log.Error("Metadata component not found");
                            return;
                        }

                        var light = EnsureComp<MapLightComponent>(realMapUid);
                        light.AmbientLightColor = comp.SunlightColor;

                        Dirty(realMapUid, light, metadata);
                    }
                }

                Log.Info("Weather set");
            }

            if (!comp.Lightning)
            {
                 foreach (var thunder in EntityManager.EntityQuery<LightningMarkerComponent>())
                {
                      thunder.Cleared = true;
                }
            }

        }

        protected override void Ended(EntityUid uid, WeatherChangeRuleComponent comp, GameRuleComponent gameRule, GameRuleEndedEvent args)
        {
            base.Ended(uid, comp, gameRule, args);

            foreach (var weather in EntityManager.EntityQuery<WeatherComponent>())
            {
                var target = weather.Owner;
                if (!_prototypeManager.TryIndex<WeatherPrototype>(comp.ReturnWeather, out var returnWeather))
                {
                    Log.Error("Weather prototype not found!");
                    return;
                }
                var state = new WeatherState();
                var data = new WeatherData();

                var mapId = Transform(target).MapID;
                var mapUid = Transform(target).MapUid;

                _weather.SetWeather(mapId, returnWeather, TimeSpan.FromMinutes(99999));
                Log.Info("Weather set");

                if (comp.Sunlight)
                {
                    if (mapUid.HasValue)
                    {
                        var realMapUid = mapUid.Value;
                        _entManager.RemoveComponent<MapLightComponent>(realMapUid);
                        // _entManager.RemoveComponent<MapGridComponent>(realMapUid); // THIS WAS A BAD IDEA OH GOD
                        // Dirty(mapUid, light, metadata);
                    }
                }
            }

            if (!comp.Lightning)
            {
                 foreach (var thunder in EntityManager.EntityQuery<LightningMarkerComponent>())
                {
                    thunder.Cleared = false;
                }
            }

        }
    }
}
