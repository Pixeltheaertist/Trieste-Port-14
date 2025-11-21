using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Weather;
using Robust.Shared.Map.Components;
using Content.Shared.Gravity;
using Robust.Shared.Prototypes;


//Summary
// This system is a simple event that will cause a weather change to occur during the event, and shift to another at the end.
// The "TargetWeather" variable will dictate what weather prototype is active during the event.
// The "ReturnWeather" variable will dictate what weather it returns to after the event (this is permanent until changed again!)
// The "Lightning" variable dictates whether or not lightning is allowed to occurr during this event, or if it should be disabled. On by default.
// The "Sunlight" variable dicatates whether the platform and waste zone will be fully covered in "light"
// The "SunlightColor" variable takes a hex code to indicate a specific color to bathe the platform in if "Sunlight" is enabled.
//Summary

namespace Content.Server.StationEvents.Events;

public sealed class WeatherChangeRule : StationEventSystem<WeatherChangeRuleComponent>
{

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedWeatherSystem _weather = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;


    protected override void Started(EntityUid uid, WeatherChangeRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        var query = EntityQueryEnumerator<WeatherComponent>();
        while (query.MoveNext(out var weatherUid, out var weather))
        {
            if (!_prototypeManager.TryIndex<WeatherPrototype>(comp.TargetWeather, out var targetWeather))
            {
                Log.Error("Weather prototype not found!");
                return;
            }

            var mapId = Transform(weatherUid).MapID;
            var mapUid = Transform(weatherUid).MapUid;

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

        var query = EntityQueryEnumerator<WeatherComponent>();
        while (query.MoveNext(out var weatherUid, out var weather))
        {
            if (!_prototypeManager.TryIndex<WeatherPrototype>(comp.ReturnWeather, out var returnWeather))
            {
                Log.Error("Weather prototype not found!");
                return;
            }

            var mapId = Transform(weatherUid).MapID;
            var mapUid = Transform(weatherUid).MapUid;

            _weather.SetWeather(mapId, returnWeather, TimeSpan.FromMinutes(99999));
            Log.Info("Weather set");

            if (!comp.Sunlight)
                continue;

            if (!mapUid.HasValue)
                continue;

            var realMapUid = mapUid.Value;
            _entManager.RemoveComponent<MapLightComponent>(realMapUid);
            // _entManager.RemoveComponent<MapGridComponent>(realMapUid); // THIS WAS A BAD IDEA OH GOD <- lol, lmao even
            // Dirty(mapUid, light, metadata);
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
