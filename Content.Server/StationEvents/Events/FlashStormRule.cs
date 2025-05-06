using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Shared.Weather;
using Content.Server._TP;
using Robust.Shared.Map;
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


//Summary
// This code controls "Flash Storms", which are dangerous, violent storms that berate Trieste for about 3 minutes.
// At the start of the storm, lightning increases intensity and nearness to Trieste, and rain picks up into a full Storm.
// It also disables the bell.
// Halfway into the storm, every light on the platform will begin flickering and freaking out, before eventually fully shutting off.
// After this, the storm will begin tapering off, eventually returning to the normal levels of rain and lightning.
//Summary
namespace Content.Server.StationEvents.Events
{
    public sealed class FlashStormRule : StationEventSystem<FlashStormRuleComponent>
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

        private float _updateTimer = 0f;
        private const float UpdateInterval = 2f;
        public EntityUid _flickerUid = new EntityUid();
        private FlashStormRuleComponent _flickerComp = new FlashStormRuleComponent();
        private GameRuleComponent _flickerGameRule = new GameRuleComponent();


        protected override void Started(EntityUid uid, FlashStormRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
        {
            base.Started(uid, comp, gameRule, args);
            Log.Error("Flash Storm Started");

            _updateTimer = 0f;

            // Update the entity UID and components
            _flickerUid = uid;
            _flickerComp = comp;
            _flickerGameRule = gameRule;

            var _stormSong = _audio.GetSound(comp.StormMusic);
            Filter filter;
            _entManager.System<ServerGlobalSoundSystem>().PlayAdminGlobal( Filter.Empty().AddAllPlayers(_playerManager), "/Audio/StationEvents/the_approaching_storm.ogg", AudioParams.Default.WithVolume(-2f), false);

            foreach (var weather in EntityManager.EntityQuery<WeatherComponent>())
            {
                var target = weather.Owner;
                if (!_prototypeManager.TryIndex<WeatherPrototype>(comp.StormWeather, out var stormWeatherProto))
                {
                    Log.Error("Weather prototype not found!");
                    return;
                }
                var state = new WeatherState();
                var data = new WeatherData();

                var mapId = Transform(target).MapID;


                _weather.SetWeather(mapId, stormWeatherProto, TimeSpan.FromMinutes(99999));
                Log.Error("Weather set");
            }

            foreach (var thunder in EntityManager.EntityQuery<LightningMarkerComponent>())
            {
              thunder.ThunderRange = 50f; // Decrease thunder range
              thunder.ThunderFrequency = 0.5f; // Increase thunder frequency
              thunder.StormMode = true;
            }

            BeginFlicker(comp, gameRule);
        }

        private void BeginFlicker(FlashStormRuleComponent comp, GameRuleComponent gameRule)
        {
            Log.Error("beginning flicker");

            var lights = GetEntityQuery<PoweredLightComponent>();
            comp.Flickering = true;

            foreach (var thunder in EntityManager.EntityQuery<LightningMarkerComponent>())
            {
                var thunderSite = thunder.Owner;

                foreach (var light in _lookup.GetEntitiesInRange(thunderSite, 200f, LookupFlags.StaticSundries))
                {
                    if (!lights.HasComponent(light)) // Flicker lights
                        continue;
                    Log.Error("flickering");

                    _ghost.DoGhostBooEvent(light);
                }
            }

        }

        protected override void Ended(EntityUid uid, FlashStormRuleComponent comp, GameRuleComponent gameRule, GameRuleEndedEvent args)
        {
            base.Ended(uid, comp, gameRule, args);

            _updateTimer = 120f;

            Log.Error("flash storm ended");

            foreach (var bell in EntityManager.EntityQuery<BellComponent>())
            {
            if (!EntityManager.HasComponent<BellComponent>(bell.Owner))
                continue;

            // bell.CanMove = true;
            }

             foreach (var thunder in EntityManager.EntityQuery<LightningMarkerComponent>())
            {
              thunder.ThunderRange = 70f; // Normalize lightning range
              thunder.ThunderFrequency = 8f; // Normalize lightning frequency
              thunder.StormMode = false;
            }

            if (!TryGetRandomStation(out var station))
            {
                return;
            }

            if (station.HasValue)
            {
                comp.trueStation = station.Value;
            }

            foreach (var weather in EntityManager.EntityQuery<WeatherComponent>())
            {
                var target = weather.Owner;
                if (!_prototypeManager.TryIndex<WeatherPrototype>(comp.NormalWeather, out var calmWeatherProto))
                {
                    Log.Error("Weather prototype not found!");
                    return;
                }
                var state = new WeatherState();
                var data = new WeatherData();

                var mapId = Transform(target).MapID;


                _weather.SetWeather(mapId, calmWeatherProto, TimeSpan.FromMinutes(99999));
                Log.Error("Weather set");
            }

            comp.Flickering = false;
        }
    }
}
