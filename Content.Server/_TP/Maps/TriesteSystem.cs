using Content.Shared.Gravity;
using Content.Shared.Weather;
using Robust.Shared.Prototypes;

namespace Content.Server._TP.Maps;

public sealed partial class TriesteSystem : EntitySystem
{
    [Dependency] private SharedWeatherSystem _weather = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriesteComponent, MapInitEvent>(OnMapInit);
    }

    private readonly EntProtoId _stormWeather = "WeatherRain";

    private void OnMapInit(Entity<TriesteComponent> ent, ref MapInitEvent args)
    {
        var mapId = Transform(ent.Owner).MapID;
        _weather.TrySetWeather(mapId, _stormWeather, out _);
    }
}
