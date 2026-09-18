using Content.Server._TP.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared._TP.Weather;
using Content.Shared.GameTicking.Components;
using Content.Shared.Weather;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;


//Summary
// This system is a simple event that will cause a weather change to occur during the event, and shift to another at the end.
// The "TargetWeather" variable will dictate what weather prototype is active during the event.
// The "ReturnWeather" variable will dictate what weather it returns to after the event (this is permanent until changed again!)
// The "Lightning" variable dictates whether or not lightning is allowed to occurr during this event, or if it should be disabled. On by default.
// The "Sunlight" variable dicatates whether the platform and waste zone will be fully covered in "light"
// The "SunlightColor" variable takes a hex code to indicate a specific color to bathe the platform in if "Sunlight" is enabled.
//Summary

namespace Content.Server._TP.StationEvents.Events;

public sealed partial class WeatherChangeRule : StationEventSystem<WeatherChangeRuleComponent>
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private SharedWeatherSystem _weather = default!;

    protected override void Started(EntityUid uid, WeatherChangeRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        var query = EntityQueryEnumerator<WeatherStatusEffectComponent>();
        while (query.MoveNext(out var weatherUid, out _))
        {
            if (!_protoMan.TryIndex(comp.TargetWeather, out var targetWeather))
                return;

            if (!targetWeather.HasComp<WeatherStatusEffectComponent>(_factory))
            {
                Log.Error("Weather entity not found!");
                return;
            }

            var mapId = Transform(weatherUid).MapID;
            var mapUid = Transform(weatherUid).MapUid;

            _weather.TrySetWeather(mapId, targetWeather, out _, TimeSpan.FromMinutes(99999));

            if (comp.Sunlight)
            {
                if (mapUid.HasValue)
                {
                    var realMapUid = mapUid.Value;
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
            foreach (var thunder in EntityQuery<LightningMarkerComponent>())
            {
                thunder.Cleared = true;
            }
        }
    }

    protected override void Ended(EntityUid uid, WeatherChangeRuleComponent comp, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, comp, gameRule, args);

        var query = EntityQueryEnumerator<WeatherStatusEffectComponent>();
        while (query.MoveNext(out var weatherUid, out _))
        {
            if (!_protoMan.TryIndex(comp.ReturnWeather, out var returnWeather))
                return;

            if (!returnWeather.HasComp<WeatherStatusEffectComponent>(_factory))
            {
                Log.Error("Weather entity not found!");
                return;
            }

            var mapId = Transform(weatherUid).MapID;
            var mapUid = Transform(weatherUid).MapUid;

            _weather.TrySetWeather(mapId, returnWeather, out _, TimeSpan.FromMinutes(99999));
            Log.Info("Weather set");

            if (!comp.Sunlight)
                continue;

            if (!mapUid.HasValue)
                continue;

            var realMapUid = mapUid.Value;
            _entMan.RemoveComponent<MapLightComponent>(realMapUid);
            // _entManager.RemoveComponent<MapGridComponent>(realMapUid); // THIS WAS A BAD IDEA OH GOD <- lol, lmao even
            // Dirty(mapUid, light, metadata);
        }

        if (!comp.Lightning)
        {
            foreach (var thunder in EntityQuery<LightningMarkerComponent>())
            {
                thunder.Cleared = false;
            }
        }
    }
}
