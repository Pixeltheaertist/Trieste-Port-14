using Content.Server._TP.StationEvents.Events;
using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;

namespace Content.Server._TP.StationEvents.Components;


[RegisterComponent, Access(typeof(WeatherChangeRule))]
public sealed partial class WeatherChangeRuleComponent : Component
{
   // The weather that is enabled during the storm
   [DataField(required: true)]
   public EntProtoId TargetWeather = "WeatherStorm";

   // The weather it will revert to at the end of the storm
   [DataField(required: true)]
   public EntProtoId ReturnWeather = "WeatherRain";

   // Is lightning enabled during the storm?
   [DataField("lightning")]
   public bool Lightning = true;

   [DataField("sunlight")]
   public bool Sunlight;

   [DataField("sunlightColor")]
   public Color SunlightColor = Color.FromHex("#D8B059");
}

