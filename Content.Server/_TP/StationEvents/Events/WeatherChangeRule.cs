using Content.Server._TP.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared._TP.Weather;
using Content.Shared.GameTicking.Components;
using Content.Shared.Light.Components;
using Content.Shared.Weather;
using Robust.Shared.Map;
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

    protected override void Started(Entity<WeatherChangeRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        if (!_protoMan.TryIndex(ent.Comp1.TargetWeather, out var targetWeather))
            return;

        if (!targetWeather.HasComp<WeatherStatusEffectComponent>(_factory))
        {
            Log.Error("Weather entity not found!");
            return;
        }

        // Collect the maps first: TrySetWeather adds/removes weather status effect entities,
        // which would modify the collection we're enumerating.
        foreach (var (mapId, mapUid) in GetWeatherMaps())
        {
            _weather.TrySetWeather(mapId, targetWeather, out _, TimeSpan.FromMinutes(99999));

            if (ent.Comp1.Sunlight)
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

                    if (HasComp<ImplicitRoofComponent>(realMapUid))
                        RemComp<ImplicitRoofComponent>(realMapUid);

                    var light = EnsureComp<MapLightComponent>(realMapUid);
                    light.AmbientLightColor = ent.Comp1.SunlightColor;

                    Dirty(realMapUid, light, metadata);
                }
            }

            Log.Info("Weather set");
        }

        if (!ent.Comp1.Lightning)
        {
            foreach (var thunder in EntityQuery<LightningMarkerComponent>())
            {
                thunder.Cleared = true;
            }
        }
    }

    protected override void Ended(Entity<WeatherChangeRuleComponent> rule, ref GameRuleEndedEvent args)
    {
        base.Ended(rule, ref args);


        if (!_protoMan.TryIndex(rule.Comp.ReturnWeather, out var returnWeather))
            return;

        if (!returnWeather.HasComp<WeatherStatusEffectComponent>(_factory))
        {
            Log.Error("Weather entity not found!");
            return;
        }

        // See Started: collect first, then change the weather.
        foreach (var (mapId, mapUid) in GetWeatherMaps())
        {
            _weather.TrySetWeather(mapId, returnWeather, out _, TimeSpan.FromMinutes(99999));
            Log.Info("Weather set");

            if (!rule.Comp.Sunlight)
                continue;

            if (!mapUid.HasValue)
                continue;

            var realMapUid = mapUid.Value;
            _entMan.RemoveComponent<MapLightComponent>(realMapUid);
            // _entManager.RemoveComponent<MapGridComponent>(realMapUid); // THIS WAS A BAD IDEA OH GOD <- lol, lmao even
            // Dirty(mapUid, light, metadata);
        }

        if (!rule.Comp.Lightning)
        {
            foreach (var thunder in EntityQuery<LightningMarkerComponent>())
            {
                thunder.Cleared = false;
            }
        }
    }

    /// <summary>
    /// The maps that currently have weather, one entry per map.
    /// </summary>
    private List<(MapId MapId, EntityUid? MapUid)> GetWeatherMaps()
    {
        var maps = new List<(MapId, EntityUid?)>();
        var seen = new HashSet<MapId>();
        var query = EntityQueryEnumerator<WeatherStatusEffectComponent>();
        while (query.MoveNext(out var weatherUid, out _))
        {
            var xform = Transform(weatherUid);
            if (seen.Add(xform.MapID))
                maps.Add((xform.MapID, xform.MapUid));
        }

        return maps;
    }
}
