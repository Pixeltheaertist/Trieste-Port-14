using Content.Server.Administration.Logs;
using Content.Server.Audio;
using Content.Server.Ghost;
using Content.Server.StationEvents.Events;
using Content.Shared._EmberFall.Bell.Components;
using Content.Shared._TP.Weather;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Light.Components;
using Content.Shared.Weather;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;


//Summary
// This code controls "Flash Storms", which are dangerous, violent storms that berate Trieste for about 3 minutes.
// At the start of the storm, lightning increases intensity and nearness to Trieste, and rain picks up into a full Storm.
// It also disables the bell.
// Halfway into the storm, every light on the platform will begin flickering and freaking out, before eventually fully shutting off.
// After this, the storm will begin tapering off, eventually returning to the normal levels of rain and lightning.
//Summary
namespace Content.Server._TP.StationEvents.Events;

public sealed partial class FlashStormRule : StationEventSystem<Components.FlashStormRuleComponent>
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private GhostSystem _ghost = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedGameTicker _gameTicker = default!;
    [Dependency] private ServerGlobalSoundSystem _sound = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedWeatherSystem _weather = default!;
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    protected override void Started(EntityUid uid, Components.FlashStormRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);
        Log.Warning("Flash Storm started.");
        _adminLogger.Add(LogType.EventStarted, LogImpact.Extreme, $"{ToPrettyString(uid)} Flash Storm started.");

        _audio.ResolveSound(comp.StormMusic);
        _entManager.System<ServerGlobalSoundSystem>()
            .PlayAdminGlobal(Filter.Empty()
                .AddAllPlayers(_playerManager),
                "/Audio/StationEvents/the_approaching_storm.ogg",
                AudioParams.Default.WithVolume(-2f),
                false);

        if (!_protoMan.TryIndex(comp.StormWeather, out var stormWeather))
            return;

        if (!stormWeather.HasComp<WeatherStatusEffectComponent>(_factory))
        {
            Log.Error("Weather prototype not found!");
            return;
        }

        var mapIds = new HashSet<MapId>();
        var query = EntityQueryEnumerator<WeatherStatusEffectComponent>();
        while (query.MoveNext(out var target, out _))
        {
            mapIds.Add(Transform(target).MapID);
        }

        foreach (var mapId in mapIds)
        {
            _weather.TrySetWeather(mapId, stormWeather, out _, TimeSpan.FromMinutes(99999));
            Log.Error("Weather set");
        }

        foreach (var thunder in EntityQuery<LightningMarkerComponent>())
        {
            thunder.ThunderRange = 50f;
            thunder.ThunderFrequency = 0.5f;
            thunder.StormMode = true;
        }

        BeginFlicker(comp, gameRule);
    }

    private void BeginFlicker(Components.FlashStormRuleComponent comp, GameRuleComponent gameRule)
    {
        Log.Error("beginning flicker");

        var lights = GetEntityQuery<PoweredLightComponent>();
        comp.Flickering = true;

        var query = EntityQueryEnumerator<LightningMarkerComponent>();
        while (query.MoveNext(out var thunderSite, out _))
        {
            foreach (var light in _lookup.GetEntitiesInRange(thunderSite, 200f, LookupFlags.StaticSundries))
            {
                if (!lights.HasComponent(light)) // Flicker lights
                    continue;

                _ghost.DoGhostBooEvent(light);
            }
        }
    }

    protected override void Ended(EntityUid uid, Components.FlashStormRuleComponent comp, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, comp, gameRule, args);

        Log.Error("flash storm ended");
        _adminLogger.Add(LogType.EventStarted, LogImpact.Extreme, $"{ToPrettyString(uid)} Flash Storm ended.");

        foreach (var thunder in EntityQuery<Shared._TP.Weather.LightningMarkerComponent>())
        {
            thunder.ThunderRange = 70f;
            thunder.ThunderFrequency = 8f;
            thunder.StormMode = false;
        }

        if (!TryGetRandomStation(out var station))
            return;

        if (station.HasValue)
            comp.TrueStation = station.Value;

        if (!_protoMan.TryIndex(comp.NormalWeather, out var normalWeather))
            return;

        if (!normalWeather.HasComp<WeatherStatusEffectComponent>(_factory))
        {
            Log.Error("Weather prototype not found!");
            return;
        }

        var mapIds = new HashSet<MapId>();
        var query = EntityQueryEnumerator<WeatherStatusEffectComponent>();
        while (query.MoveNext(out var target, out _))
        {
            mapIds.Add(Transform(target).MapID);
        }

        foreach (var mapId in mapIds)
        {
            _weather.TrySetWeather(mapId, normalWeather, out _, TimeSpan.FromMinutes(99999));
            Log.Error("Weather set");
        }

        comp.Flickering = false;
    }
}
